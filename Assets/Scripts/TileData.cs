// Egy cella LOGIKAI adata – ez a "source of truth" a játékmenethez.
// A Tilemap csak megjeleníti; a tényleges állapotot itt tároljuk.
// Idle farmnál ide jön majd: milyen növény, mikor ültették, hányadik növekedési fázis...
[System.Serializable]
public class TileData
{
    public TileType type;

    // Farm-specifikus állapot (később bővíthető):
    public bool hasCrop;        // van-e rajta ültetett növény
    public int growthStage;     // hányadik növekedési fázisban van

    public TileData(TileType type)
    {
        this.type = type;
    }
}
