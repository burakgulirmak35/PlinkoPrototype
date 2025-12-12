namespace PlinkoPrototype
{
    public enum GameState
    {
        Idle,       // Oyun başlamadı, tap bekleniyor
        Playing,    // Toplar atılıyor
        LevelEnd,   // Level bitiş animasyonu / bekleme
        Reset       // 15 dk reset süreci
    }
}
