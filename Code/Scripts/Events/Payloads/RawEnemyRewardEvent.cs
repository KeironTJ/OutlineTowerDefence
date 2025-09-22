public struct RawEnemyRewardEvent
{
    public string definitionId;
    public int wave;
    public int fragments;
    public int cores;
    public int prisms;
    public int loops;
    public bool isBoss;
    public RawEnemyRewardEvent(string id, int wave, int fr, int co, int pr, int lo, bool isBoss)
    {
        definitionId = id;
        this.wave = wave;
        fragments = fr;
        cores = co;
        prisms = pr;
        loops = lo;
        this.isBoss = isBoss;
    }
}