using System.Collections.Generic;
using UnityEngine;

public class SpatialHashGrid<T>
{

    private class HashGridBucket
    {
        private Dictionary<T, int> itemIndices;
        private List<T> items;
        private bool isEmpty;

        public bool IsEmpty => isEmpty;

        public HashGridBucket()
        {
            itemIndices = new Dictionary<T, int>();
            items = new List<T>();
            isEmpty = true;
        }

        public List<T> GetItems()
        {
            return items;
        }

        public void Add(T item)
        {
            if (!itemIndices.ContainsKey(item))
            {
                items.Add(item);
                itemIndices.Add(item, items.Count - 1);
                
                if (isEmpty)
                {
                    isEmpty = false;
                }
            }
        }

        public void Remove(T item)
        {
            if (!isEmpty && itemIndices.ContainsKey(item))
            {
                int i = itemIndices[item];
                int lastIndex = items.Count - 1;
                
                T temp = items[i];
                items[i] = items[lastIndex];
                items[lastIndex] = temp;
                itemIndices[items[i]] = i;
                
                items.RemoveAt(lastIndex);
                itemIndices.Remove(item);

                if (items.Count == 0)
                {
                    isEmpty = true;
                }
            }
        }
    }
    
    private Dictionary<T, int[]> itemLocationsInGrid;
    private HashGridBucket[,] grid;
    private Vector3 gridBottomLeft;
    private int[] gridSize;
    private float tileSize;
    private int gridSizeX;
    private int gridSizeZ;
    
    public SpatialHashGrid(float tileSize, Vector2 gridWorldSize, Vector3 gridPosition)
    {
        itemLocationsInGrid = new Dictionary<T, int[]>();
        gridSizeX = Mathf.CeilToInt(gridWorldSize.x / tileSize);
        gridSizeZ = Mathf.CeilToInt(gridWorldSize.y / tileSize);
        grid = new HashGridBucket[gridSizeX, gridSizeZ];
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                grid[x, z] = new HashGridBucket();
            }
        }
        gridBottomLeft = gridPosition - tileSize * (Vector3.right * gridSizeX / 2f + Vector3.forward * gridSizeZ / 2f);
        this.tileSize = tileSize;
    }

    private int[] Hash(Vector3 location)
    {
        int x = (int) ((location.x - gridBottomLeft.x) / tileSize);
        int z = (int) ((location.z - gridBottomLeft.z) / tileSize);

        return new[] {x, z};
    }
    
    public void Add(T item, Vector3 location)
    {
        if (!itemLocationsInGrid.ContainsKey(item))
        {
            int[] i = Hash(location);
        
            itemLocationsInGrid.Add(item, new [] {i[0], i[1]});
            grid[i[0], i[1]].Add(item);
        }
        
    }

    public void Remove(T item)
    {
        if (itemLocationsInGrid.ContainsKey(item))
        {
            int[] i = itemLocationsInGrid[item];
        
            itemLocationsInGrid.Remove(item);
            grid[i[0], i[1]].Remove(item);
        }
    }

    public void Rehash(T item, Vector3 location)
    {
        Remove(item);
        Add(item, location);
    }
    
    public List<List<T>> GetINearbyItems(Vector3 location, int radius)
    {
        //radius can't be even
        if (radius % 2 == 0)
        {
            radius++;
        }
        
        List<List<T>> output = new List<List<T>>((2 * radius + 1) * (2 * radius + 1));
        
        int[] i = Hash(location);
        int xIndex;
        int zIndex;
        for (int x = 0; x < 2 * radius + 1; x++)
        {
            for (int z = 0; z < 2 * radius + 1; z++)
            {
                //Clamp indices to array bounds
                xIndex = i[0] - radius + x;
                xIndex = xIndex < 0 ? 0 : xIndex > gridSizeX - 1 ? gridSizeX - 1 : xIndex;
                zIndex = i[1] - radius + z;
                zIndex = zIndex < 0 ? 0 : zIndex > gridSizeX - 1 ? gridSizeX - 1 : zIndex;

                if (!grid[xIndex, zIndex].IsEmpty)
                {
                    output.Add(grid[xIndex, zIndex].GetItems());
                }
            }
        }

        return output;
    }
}
