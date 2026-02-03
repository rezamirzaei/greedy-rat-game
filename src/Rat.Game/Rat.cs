namespace Rat.Game;

public sealed class Rat
{
    public Rat(int maxHealth)
    {
        if (maxHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be positive.");

        MaxHealth = maxHealth;
        Health = maxHealth;
    }

    public int MaxHealth { get; private set; }
    public int Health { get; private set; }
    public Position Position { get; private set; }

    public bool IsAlive => Health > 0;
    
    // New properties for enhanced gameplay
    public int Score { get; private set; }
    public int GemsCollected { get; private set; }
    public int ShotsAvoided { get; private set; }
    public int ComboCount { get; private set; }
    public int MaxCombo { get; private set; }
    public int RockDigsRemaining { get; private set; }
    public double ShieldSecondsRemaining { get; private set; }
    public double SpeedBoostSecondsRemaining { get; private set; }
    public int TotalMoves { get; private set; }
    
    public bool HasShield => ShieldSecondsRemaining > 0;
    public bool HasSpeedBoost => SpeedBoostSecondsRemaining > 0;
    public bool CanDigRock => RockDigsRemaining > 0;

    public void Reset(Position start)
    {
        Position = start;
        Health = MaxHealth;
        ComboCount = 0;
        RockDigsRemaining = 1; // Start each chapter with 1 rock dig
        ShieldSecondsRemaining = 0;
        SpeedBoostSecondsRemaining = 0;
    }
    
    public void FullReset()
    {
        Health = MaxHealth;
        Score = 0;
        GemsCollected = 0;
        ShotsAvoided = 0;
        ComboCount = 0;
        MaxCombo = 0;
        TotalMoves = 0;
        RockDigsRemaining = 1;
        ShieldSecondsRemaining = 0;
        SpeedBoostSecondsRemaining = 0;
    }

    public void MoveTo(Position position)
    {
        Position = position;
        TotalMoves++;
    }

    public void Damage(int amount)
    {
        if (amount <= 0)
            return;

        // Shield blocks damage
        if (HasShield)
        {
            ShieldSecondsRemaining = 0; // Shield breaks after blocking
            return;
        }

        Health = Math.Max(0, Health - amount);
        ComboCount = 0; // Reset combo on taking damage
    }
    
    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        Health = Math.Min(MaxHealth, Health + amount);
    }
    
    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0)
            return;
            
        MaxHealth += amount;
        Health = Math.Min(MaxHealth, Health + amount);
    }
    
    public void AddScore(int points)
    {
        if (points > 0)
            Score += points;
    }
    
    public void CollectGem()
    {
        GemsCollected++;
        AddScore(100 + ComboCount * 10); // Bonus for combo
    }
    
    public void RecordShotAvoided(int shotCount)
    {
        ShotsAvoided += shotCount;
        ComboCount += shotCount;
        MaxCombo = Math.Max(MaxCombo, ComboCount);
        AddScore(shotCount * 5 * (1 + ComboCount / 5)); // Progressive combo bonus
    }
    
    public void AddRockDigs(int count)
    {
        RockDigsRemaining += count;
    }
    
    public bool UseRockDig()
    {
        if (RockDigsRemaining <= 0)
            return false;
            
        RockDigsRemaining--;
        return true;
    }
    
    public void ActivateShield(double seconds)
    {
        ShieldSecondsRemaining = Math.Max(ShieldSecondsRemaining, seconds);
    }
    
    public void ActivateSpeedBoost(double seconds)
    {
        SpeedBoostSecondsRemaining = Math.Max(SpeedBoostSecondsRemaining, seconds);
    }
    
    public void UpdateTimers(double deltaSeconds)
    {
        if (ShieldSecondsRemaining > 0)
            ShieldSecondsRemaining = Math.Max(0, ShieldSecondsRemaining - deltaSeconds);
            
        if (SpeedBoostSecondsRemaining > 0)
            SpeedBoostSecondsRemaining = Math.Max(0, SpeedBoostSecondsRemaining - deltaSeconds);
    }
}

