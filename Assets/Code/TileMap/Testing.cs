﻿using Assets.Code.Grid;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Code.TileMap
{
    public class Testing : MonoBehaviour
    {
        private void Start()
        {
            Grid<TileMapNode> grid = new(50, 50, 1, (g, x, y) => new(x, y, new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)), new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)), true));
            var mesh = grid.CreateMesh();
            GetComponent<MeshFilter>().mesh = mesh;
            Pathfinding pathfinding = new(grid);
        }
    }
}
