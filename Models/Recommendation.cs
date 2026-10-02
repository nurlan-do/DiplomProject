namespace DiplomBackend.Models;

public class Recommendation
{
    public Profession Profession { get; set; }
    public int MatchPercent { get; set; }

    public Recommendation(Profession profession, int matchPercent)
    {
        Profession = profession;
        MatchPercent = matchPercent;
    }
}
