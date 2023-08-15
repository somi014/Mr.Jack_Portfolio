using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Node
{
    public Node(bool wall, int posX, int posY)
    {
        isWall = wall;
        x = posX;
        y = posY;
    }

    public bool isWall;
    public Node ParentNode;

    // G : 시작으로부터 이동했던 거리, H : |가로|+|세로| 장애물 무시하여 목표까지의 거리, F : G + H
    public int x, y, G, H;
    public int F
    {
        get
        {
            return G + H;
        }
    }
}

public class AStarFindPath : MonoBehaviour
{
    public Vector2Int bottomLeft = new Vector2Int(0, 0);
    public Vector2Int topRight = new Vector2Int(8, 12);
    private List<Node> FinalNodeList;
    public List<Node> GetNodeList
    {
        get => FinalNodeList;
    }

    //public bool allowDiagonal = true;
    //public bool dontCrossCorner = true;

    Node[,] NodeArray;
    Node StartNode, TargetNode, CurNode;
    List<Node> OpenList, ClosedList;

    public void SetNode(Node[,] node)
    {
        NodeArray = node;
    }

    public void PathFinding(Vector3Int startPos, Vector3Int targetPos)
    {
        // 시작과 끝 노드, 열린리스트와 닫힌리스트, 마지막리스트 초기화
        StartNode = NodeArray[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
        TargetNode = NodeArray[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

        OpenList = new List<Node>() { StartNode };
        ClosedList = new List<Node>();
        FinalNodeList = new List<Node>();


        while (OpenList.Count > 0)
        {
            // 열린리스트 중 가장 F가 작고 F가 같다면 H가 작은 걸 현재노드로 하고 열린리스트에서 닫힌리스트로 옮기기
            CurNode = OpenList[0];
            for (int i = 1; i < OpenList.Count; i++)
            {

                if (OpenList[i].H < CurNode.H)
                {
                    CurNode = OpenList[i];
                }
            }
            OpenList.Remove(CurNode);
            ClosedList.Add(CurNode);

            // 마지막
            if (CurNode == TargetNode)
            {
                Node TargetCurNode = TargetNode;
                while (TargetCurNode != StartNode)
                {
                    FinalNodeList.Add(TargetCurNode);
                    TargetCurNode = TargetCurNode.ParentNode;
                }
                FinalNodeList.Add(StartNode);
                FinalNodeList.Reverse();

#if UNITY_EDITOR
                for (int i = 0; i < FinalNodeList.Count; i++)
                {
                    Debug.Log(i + "번째는 " + FinalNodeList[i].x + ", " + FinalNodeList[i].y);
                }
#endif
                return;
            }

            if (CurNode.y % 2 == 0)    //y 짝수
            {
                OpenListAdd(CurNode.x + 1, CurNode.y);
                OpenListAdd(CurNode.x, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y);
                OpenListAdd(CurNode.x - 1, CurNode.y - 1);
                OpenListAdd(CurNode.x, CurNode.y - 1);
            }
            else                        //y 홀수 
            {
                OpenListAdd(CurNode.x + 1, CurNode.y);
                OpenListAdd(CurNode.x + 1, CurNode.y + 1);
                OpenListAdd(CurNode.x, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y);
                OpenListAdd(CurNode.x, CurNode.y - 1);
                OpenListAdd(CurNode.x + 1, CurNode.y - 1);
            }
        }
    }

    void OpenListAdd(int checkX, int checkY)
    {
        if (checkX >= topRight.x + 1 || checkY >= topRight.y + 1)
        {
            return;
        }

        // 상하좌우 범위를 벗어나지 않고, 벽이 아니면서, 닫힌리스트에 없다면
        if (checkX >= bottomLeft.x && checkX < topRight.x + 1 && checkY >= bottomLeft.y && checkY < topRight.y + 1
            && NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y].isWall == false
            && ClosedList.Contains(NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y]) == false)
        {
            // 이웃노드에 넣고 비용은 10
            Node NeighborNode = NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y];
            int MoveCost = CurNode.G + 10;

            // 이동비용이 이웃노드G보다 작거나 또는 열린리스트에 이웃노드가 없다면 G, H, ParentNode를 설정 후 열린리스트에 추가
            if (MoveCost < NeighborNode.G || OpenList.Contains(NeighborNode) == false)
            {
                NeighborNode.G = MoveCost;
                NeighborNode.H = (Mathf.Abs(NeighborNode.x - TargetNode.x) + Mathf.Abs(NeighborNode.y - TargetNode.y)) * 10;
                NeighborNode.ParentNode = CurNode;

                OpenList.Add(NeighborNode);
            }
        }
    }
}