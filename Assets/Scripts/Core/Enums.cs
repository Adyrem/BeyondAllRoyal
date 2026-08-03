public enum EntityType
{
    Soldier             = 0,
    HeavyGunner         = 1,
    ExplosiveSpecialist = 2,
    Hovercraft          = 3,
    HeavyTank           = 4,
    MachinegunTurret    = 5,
    RailgunTurret       = 6
}

public enum Owner { Player, NPC }

public enum GameState { Pregame, InGame, Victory, Defeat }

public enum CounterResult { Even, Strong, Weak }

public enum GameMode { Singleplayer, Multiplayer }

public enum AIDifficulty { Easy, Medium, Hard }
