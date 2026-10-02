namespace DiplomBackend.Models;

public class UserProfile
{
    public int Age { get; set; }
    public string Education { get; set; }
    public List<string> Interests { get; set; }
    public List<string> Skills { get; set; }

    public UserProfile()
    {
        Education = "";
        Interests = new List<string>();
        Skills = new List<string>();
    }
}
