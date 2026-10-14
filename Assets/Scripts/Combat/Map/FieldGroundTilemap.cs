using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 반복되는 바닥만 청크 렌더링으로 합치고 입체 장식은 그대로 둡니다.
public sealed class FieldGroundTilemap : MonoBehaviour
{
    private readonly List<Tile> ownedTiles = new List<Tile>();

    public void Build(GameObject map)
    {
        var grid = new GameObject("Ground Grid", typeof(Grid));
        grid.transform.SetParent(transform, false);
        grid.transform.rotation = Quaternion.Euler(90, 0, 0);
        grid.GetComponent<Grid>().cellSize = new Vector3(2, 2, 1);
        var layers = new Dictionary<int, Tilemap>();
        var tiles = new Dictionary<Sprite, Tile>();
        foreach (var source in map.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!source.name.StartsWith("Garden Grass", System.StringComparison.Ordinal) || source.sprite == null || source.transform.childCount != 0) continue;
            if (!layers.TryGetValue(source.sortingOrder, out var layer))
            {
                var obj = new GameObject("Grass Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
                obj.layer = source.gameObject.layer;
                obj.transform.SetParent(grid.transform, false);
                layer = obj.GetComponent<Tilemap>();
                var renderer = obj.GetComponent<TilemapRenderer>();
                renderer.mode = TilemapRenderer.Mode.Chunk;
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder;
                renderer.sharedMaterial = source.sharedMaterial;
                layers.Add(source.sortingOrder, layer);
            }
            var cell = layer.WorldToCell(source.transform.position);
            if (layer.HasTile(cell))
            {
                source.gameObject.SetActive(false);
                Destroy(source.gameObject);
                continue;
            }
            if (!tiles.TryGetValue(source.sprite, out var tile))
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = source.sprite;
                tile.flags = TileFlags.None;
                tile.colliderType = Tile.ColliderType.None;
                tiles.Add(source.sprite, tile);
                ownedTiles.Add(tile);
            }
            layer.SetTile(cell, tile);
            layer.SetColor(cell, source.color);
            Vector3 anchor = layer.CellToLocalInterpolated((Vector3)cell + layer.tileAnchor);
            layer.SetTransformMatrix(cell, Matrix4x4.Translate(-anchor) * layer.transform.worldToLocalMatrix * source.transform.localToWorldMatrix);
            source.gameObject.SetActive(false);
            Destroy(source.gameObject);
        }
    }

    private void OnDestroy()
    {
        foreach (var tile in ownedTiles) if (tile != null) Destroy(tile);
    }
}
