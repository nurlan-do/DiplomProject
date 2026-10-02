//using DiplomBackend.Models;

//namespace DiplomBackend.Services;

//public class RecommendationService
//{
//    private ProfessionService professionService;

//    public RecommendationService(ProfessionService professionService)
//    {
//        this.professionService = professionService;
//    }

//    public List<Recommendation> GetRecommendations(UserProfile profile)
//    {
//        List<Recommendation> recommendations = new List<Recommendation>();

//        foreach (Profession profession in professionService.GetAll())
//        {
//            int points = 0;

//            foreach (string interest in profile.Interests)
//            {
//                foreach (string professionInterest in profession.Interests)
//                {
//                    if (interest.Equals(
//                        professionInterest,
//                        StringComparison.OrdinalIgnoreCase))
//                    {
//                        points += 30;
//                    }
//                }
//            }

//            foreach (string skill in profile.Skills)
//            {
//                foreach (string requiredSkill in profession.RequiredSkills)
//                {
//                    if (skill.Equals(
//                        requiredSkill,
//                        StringComparison.OrdinalIgnoreCase))
//                    {
//                        points += 20;
//                    }
//                }
//            }

//            if (points > 100)
//                points = 100;

//            if (points > 0)
//            {
//                recommendations.Add(
//                    new Recommendation(profession, points)
//                );
//            }
//        }

//        return recommendations;
//    }
//}
