using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class NodeHeap
{
    private List<NavNode> nodes;
    private int itemCount;

    public int Count => itemCount;

    public NodeHeap()
    {
        nodes = new List<NavNode>();
    }

    public void Add(NavNode node)
    {
        if (nodes.Count <= itemCount)
        {
            nodes.Add(node);
        }
        else
        {
            nodes[itemCount] = node;
        }
        
        node.heapIndex = itemCount;
        itemCount++;
        BubbleUp(node);
    }

    public NavNode Pop()
    {
        NavNode topNode = nodes[0];
        
        itemCount--;
        nodes[0] = nodes[itemCount];
        nodes[0].heapIndex = 0;
        SortDown(nodes[0]);
        
        return topNode;
    }

    public bool Contains(NavNode node)
    {
        if (nodes.Count > node.heapIndex)
        {
            return Equals(nodes[node.heapIndex], node);
        }

        return false;
    }

    private void SortDown(NavNode node)
    {
        int leftChildIndex = node.heapIndex * 2 + 1;
        int rightChildIndex = node.heapIndex * 2 + 2;

        while (leftChildIndex < itemCount)
        {
            int swapIndex = leftChildIndex;

            if (rightChildIndex < itemCount)
            {
                if (nodes[leftChildIndex].CompareTo(nodes[rightChildIndex]) < 0)
                {
                    swapIndex = rightChildIndex;
                }
            }

            if (node.CompareTo(nodes[swapIndex]) < 0)
            {
                Swap(node, nodes[swapIndex]);
                
                leftChildIndex = node.heapIndex * 2 + 1;
                rightChildIndex = node.heapIndex * 2 + 2;
            }
            else
            {
                return;
            }
        }
    }

    private void BubbleUp(NavNode node)
    {
        int parentIndex = (node.heapIndex - 1) / 2;
        NavNode parent = nodes[parentIndex];

        while (parentIndex >= 0 && node.CompareTo(parent) > 0)
        {
            Swap(node, parent);
            
            parentIndex = (node.heapIndex - 1) / 2;
            parent = nodes[parentIndex];
        }

    }

    private void Swap(NavNode nodeA, NavNode nodeB)
    {
        nodes[nodeA.heapIndex] = nodeB;
        nodes[nodeB.heapIndex] = nodeA;
        
        int tempIndex = nodeA.heapIndex;
        nodeA.heapIndex = nodeB.heapIndex;
        nodeB.heapIndex = tempIndex;
    }
}
