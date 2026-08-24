using UnityEngine;
using UnityEngine.Tilemaps;

// Ideiglenes, kódból generált tile-ok, hogy AZONNAL lásd a rácsot,
// még mielőtt kész pixel art sprite-jaid lennének.
// Minden típushoz egy egyszínű, vékony rácsvonalas négyzetet készít.
// Ha meglesz az igazi grafika, ezt lecseréled valódi Tile assetekre
// (lásd FarmGrid.tileVisuals).
public static class PlaceholderTileFactory
{
    // Egy típushoz tartozó kész (cache-elt) Tile.
    public static Tile Create(int tileSizePx, Color fill)
    {
        var tex = new Texture2D(tileSizePx, tileSizePx, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,   // pixel art: NE legyen elmosás
            wrapMode = TextureWrapMode.Clamp
        };

        // Kicsit sötétebb rácsvonal a szélén, hogy látszódjanak a cellák.
        Color border = fill * 0.75f;
        border.a = 1f;

        for (int y = 0; y < tileSizePx; y++)
        {
            for (int x = 0; x < tileSizePx; x++)
            {
                bool edge = x == 0 || y == 0 || x == tileSizePx - 1 || y == tileSizePx - 1;
                tex.SetPixel(x, y, edge ? border : fill);
            }
        }
        tex.Apply();

        // A PPU = textúra mérete => 1 tile pontosan 1 world unit (egyezik a Grid cellSize-zal).
        // Pl. 64px textúra @ 64 PPU = 1 unit.
        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tileSizePx, tileSizePx),
            new Vector2(0.5f, 0.5f),
            tileSizePx);

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        return tile;
    }
}
