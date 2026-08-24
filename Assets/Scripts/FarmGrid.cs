using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// A farm pálya KÖZPONTI rendszere (hibrid megoldás):
//  - Tilemap  => a megjelenítés (gyors, sok cellát bír)
//  - TileData[,] => a logikai adat (source of truth a játékmenethez)
//
// Használat:
//  1) Üres GameObject a scene-ben  ->  add hozzá ezt a scriptet
//  2) Play  ->  legenerál egy 40x26-os rácsot fűvel + pár ösvénnyel
//  3) Bal klikk: farmland lerakása, jobb klikk: vissza fűre
//
// Amikor meglesz a pixel art: a lenti "tileVisuals" listába húzod be
// a valódi Tile asseteket típusonként, és eltűnnek a placeholderek.
public class FarmGrid : MonoBehaviour
{
    [Header("Rács mérete")]
    public int width = 40;
    public int height = 26;

    [Header("Pixel art beállítás")]
    [Tooltip("Egy tile mérete pixelben (a placeholderekhez). Ez legyen egyenlő a sprite-jaid PPU értékével.")]
    public int tileSizePx = 64;

    [Header("Megjelenítés")]
    [Tooltip("Ide húzd be a Grid alatti Tilemapot. Ha üres, a script automatikusan létrehoz egyet.")]
    public Tilemap groundTilemap;

    [Tooltip("Igazi grafika hozzárendelése típusonként. Amelyik üres, oda placeholder generálódik.")]
    public List<TileVisual> tileVisuals = new List<TileVisual>();

    [Header("Kamera")]
    [Tooltip("Play-nél a kamerát a rács közepére igazítja, hogy minden látszódjon.")]
    public bool centerCameraOnStart = true;

    // A logikai adatréteg – ebben él a pálya "igazsága".
    private TileData[,] tiles;

    // Típus -> megjelenítendő Tile (valódi vagy generált placeholder).
    private readonly Dictionary<TileType, TileBase> tileLookup = new Dictionary<TileType, TileBase>();

    [System.Serializable]
    public struct TileVisual
    {
        public TileType type;
        public TileBase tile;   // Assets-ből behúzott valódi Tile (opcionális)
    }

    // Placeholder színek típusonként (csak amíg nincs igazi art).
    private static readonly Dictionary<TileType, Color> PlaceholderColors = new Dictionary<TileType, Color>
    {
        { TileType.Grass,    new Color(0.36f, 0.62f, 0.28f) }, // zöld
        { TileType.Path,     new Color(0.78f, 0.66f, 0.45f) }, // homokos barna
        { TileType.Farmland, new Color(0.45f, 0.31f, 0.20f) }, // felszántott sötétbarna
        { TileType.Water,    new Color(0.30f, 0.55f, 0.80f) }, // víz kék
    };

    private void Awake()
    {
        EnsureTilemap();
        BuildTileLookup();
        GenerateInitialMap();
        RenderAll();
        if (centerCameraOnStart) CenterCamera();
    }

    private void Update()
    {
        HandleMouse();
    }

    // ---------------------------------------------------------------------
    // Felépítés
    // ---------------------------------------------------------------------

    // Ha nincs Tilemap behúzva, csinálunk egyet kódból (Grid + Tilemap + renderer).
    private void EnsureTilemap()
    {
        if (groundTilemap != null) return;

        var gridGo = new GameObject("Grid");
        gridGo.transform.SetParent(transform, false);
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 0); // 64px tile @ 64 PPU = 1 unit

        var tmGo = new GameObject("Ground");
        tmGo.transform.SetParent(gridGo.transform, false);
        groundTilemap = tmGo.AddComponent<Tilemap>();
        var renderer = tmGo.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 0;
    }

    // Minden típushoz eltároljuk a megjelenítendő tile-t.
    // Ha van valódi behúzva -> azt; különben generált placeholder.
    private void BuildTileLookup()
    {
        tileLookup.Clear();

        // Először a kézzel behúzott valódi tile-ok.
        foreach (var v in tileVisuals)
        {
            if (v.tile != null) tileLookup[v.type] = v.tile;
        }

        // A maradékhoz placeholder.
        foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
        {
            if (tileLookup.ContainsKey(type)) continue;
            Color c = PlaceholderColors.TryGetValue(type, out var col) ? col : Color.magenta;
            tileLookup[type] = PlaceholderTileFactory.Create(tileSizePx, c);
        }
    }

    // Kezdő pálya: minden fű, plusz egy kereszt alakú ösvény demónak.
    private void GenerateInitialMap()
    {
        tiles = new TileData[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                tiles[x, y] = new TileData(TileType.Grass);

        // Vízszintes és függőleges ösvény középen (csak hogy legyen mit nézni).
        int midY = height / 2;
        int midX = width / 2;
        for (int x = 0; x < width; x++) tiles[x, midY].type = TileType.Path;
        for (int y = 0; y < height; y++) tiles[midX, y].type = TileType.Path;
    }

    private void RenderAll()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                RenderCell(x, y);
    }

    private void RenderCell(int x, int y)
    {
        groundTilemap.SetTile(new Vector3Int(x, y, 0), tileLookup[tiles[x, y].type]);
    }

    // ---------------------------------------------------------------------
    // Publikus API (innen fogod majd használni a játékmenetből)
    // ---------------------------------------------------------------------

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    public TileData GetTile(int x, int y) => InBounds(x, y) ? tiles[x, y] : null;

    public void SetTileType(int x, int y, TileType type)
    {
        if (!InBounds(x, y)) return;
        tiles[x, y].type = type;
        RenderCell(x, y);
    }

    // Világkoordináta -> rács index.
    public Vector2Int WorldToGrid(Vector3 world)
    {
        Vector3Int c = groundTilemap.WorldToCell(world);
        return new Vector2Int(c.x, c.y);
    }

    // Rács index -> a cella közepének világkoordinátája.
    public Vector3 GridToWorldCenter(int x, int y)
    {
        return groundTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
    }

    // ---------------------------------------------------------------------
    // Interakció
    // ---------------------------------------------------------------------

    private void HandleMouse()
    {
        bool left = GridInput.LeftClickDown();
        bool right = GridInput.RightClickDown();
        if (!left && !right) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = GridInput.MouseScreenPosition();
        screen.z = -cam.transform.position.z; // ortho: kameráig tartó távolság
        Vector3 world = cam.ScreenToWorldPoint(screen);

        Vector2Int g = WorldToGrid(world);
        if (!InBounds(g.x, g.y)) return;

        if (left) SetTileType(g.x, g.y, TileType.Farmland);
        else if (right) SetTileType(g.x, g.y, TileType.Grass);
    }

    // ---------------------------------------------------------------------
    // Kamera
    // ---------------------------------------------------------------------

    private void CenterCamera()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        // A rács közepe világkoordinátában.
        Vector3 center = GridToWorldCenter(width / 2, height / 2);
        cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);

        // Ortho méret úgy, hogy a teljes rács kiférjen (magasságra ÉS szélességre is).
        float halfH = height / 2f;
        float halfW = (width / 2f) / cam.aspect;
        cam.orthographicSize = Mathf.Max(halfH, halfW) + 0.5f; // +fél cella margó
    }
}
