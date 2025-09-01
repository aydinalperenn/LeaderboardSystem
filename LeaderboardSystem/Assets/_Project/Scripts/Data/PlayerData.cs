using System;
using System.Collections.Generic;


[Serializable]
public class PlayerData
{
    public string id;
    public string nickname;
    public int score;

    [NonSerialized] public int rank;
}

[Serializable]
public class PlayerList
{
    public List<PlayerData> players;
}
