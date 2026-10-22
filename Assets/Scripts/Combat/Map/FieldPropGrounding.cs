using System.Collections.Generic;
using UnityEngine;

// 맵 그림의 투명 여백을 제외한 아래 끝을 발 위치에 맞춥니다.
// 이미지 좌표는 텍스처당 한 번만 읽고, 배치가 끝나면 CPU 복사본을 해제합니다.
public static class FieldPropGrounding
{
    public static bool Apply(GameObject map)
    {
        bool changed = false;
        var pixels = new Dictionary<Texture2D, Color32[]>();
        var bottoms = new Dictionary<Sprite, float>();
        foreach (var billboard in map.GetComponentsInChildren<FieldBillboard>(true))
        {
            if (billboard.Feet == null) continue;
            float bottom = float.PositiveInfinity;
            foreach (var renderer in billboard.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var sprite = renderer.sprite;
                if (sprite == null || !renderer.enabled || renderer.GetComponentInParent<FieldBillboard>() != billboard) continue;
                // 맵은 비패킹 스프라이트를 사용합니다. 패킹된 외부 에셋은 잘못 읽지 않습니다.
                if (sprite.packed) continue;
                if (!bottoms.TryGetValue(sprite, out float localBottom))
                {
                    var texture = sprite.texture;
                    if (!pixels.TryGetValue(texture, out var colors))
                    {
                        colors = ReadPixels(texture);
                        pixels.Add(texture, colors);
                    }
                    var rect = sprite.rect;
                    int first = -1;
                    for (int y = (int)rect.yMin; y < (int)rect.yMax && first < 0; y++)
                        for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                            if (colors[y * texture.width + x].a >= 128) { first = y; break; }
                    localBottom = first < 0 ? sprite.bounds.min.y : (first - rect.yMin - sprite.pivot.y) / sprite.pixelsPerUnit;
                    bottoms.Add(sprite, localBottom);
                }
                // 맵 아트는 회전하지 않은 자식 스프라이트로 구성됩니다.
                if (renderer.flipY) continue;
                Vector3 point = billboard.transform.InverseTransformPoint(renderer.transform.TransformPoint(new Vector3(0, localBottom, 0)));
                bottom = Mathf.Min(bottom, point.y);
            }
            if (!float.IsInfinity(bottom))
            {
                float height = -bottom * billboard.transform.lossyScale.y;
                if (!Mathf.Approximately(billboard.CenterHeight, height)) changed = true;
                billboard.CenterHeight = height;
                if (billboard.ViewCamera != null)
                {
                    var rotation = billboard.ViewCamera.transform.rotation;
                    var position = billboard.Feet.position + billboard.ViewCamera.transform.up * height;
                    if (Vector3.Distance(billboard.transform.position, position) > .0001f || Quaternion.Angle(billboard.transform.rotation, rotation) > .001f) changed = true;
                    billboard.transform.SetPositionAndRotation(position, rotation);
                }
            }
        }
        // 기존에 옆으로 밀어 둔 바닥 그림자를 각 구조물의 접지 위치로 옮깁니다.
        foreach (var obstacle in map.GetComponentsInChildren<FieldMapObstacle>(true))
        {
            Transform parent = obstacle.transform.parent;
            if (parent == null) continue;
            Transform nearest = null;
            float distance = float.PositiveInfinity;
            foreach (Transform candidate in parent)
            {
                if (candidate.name != obstacle.name + " Shadow") continue;
                float current = (candidate.position - obstacle.transform.position).sqrMagnitude;
                if (current < distance) { nearest = candidate; distance = current; }
            }
            if (nearest == null || distance > 4f) continue;
            Vector3 position = obstacle.transform.position;
            position.y = nearest.position.y;
            if ((nearest.position - position).sqrMagnitude > .000001f)
            { nearest.position = position; changed = true; }
        }
        return changed;
    }

    private static Color32[] ReadPixels(Texture2D source)
    {
        if (source.isReadable) return source.GetPixels32();
        var previous = RenderTexture.active;
        var buffer = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Texture2D copy = null;
        try
        {
            Graphics.Blit(source, buffer);
            RenderTexture.active = buffer;
            copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            return copy.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(buffer);
            if (copy != null)
            {
                if (Application.isPlaying) Object.Destroy(copy);
                else Object.DestroyImmediate(copy);
            }
        }
    }
}
