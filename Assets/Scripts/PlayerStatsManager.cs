using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PlayerStatsManager : MonoBehaviour
{
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI winsText;

    FirebaseUser user;
    FirebaseFirestore db;

    void Start()
    {
        user = FirebaseManager.Instance.User;
        db = FirebaseManager.Instance.Firestore;

        emailText.text = $"Email: {user.Email}";

        _ = LoadPlayerStats(FirebaseManager.Instance.User.UserId);
    }


    /* public async void LoadPlayerStats(string userId)
     {
         var db = FirebaseManager.Instance.Firestore;

         // stats document under the user
         DocumentReference doc = db.Collection("users")
                                   .Document(userId); // stats will be a field in this document

         try
         {
             DocumentSnapshot snap = await doc.GetSnapshotAsync();

             if (snap.Exists)
             {
                 Debug.Log(0);
                 // Try to get 'wins' field
                 int wins = 0;
                 if (snap.TryGetValue("wins", out wins))
                 {
                     winsText.text = $"Wins: {wins}";
                     Debug.Log(1);
                 }
                 else
                 {
                     winsText.text = "Wins: 0";
                     Debug.Log(2);
                 }
             }
             else
             {
                 Debug.Log(3);
                 // Document doesn't exist yet
                 winsText.text = "Wins: 0";

                 // Optionally create the document with initial stats
                 await doc.SetAsync(new Dictionary<string, object> { { "wins", 0 } });
             }
         }
         catch (Exception e)
         {
             Debug.LogError("Failed to load player stats: " + e);
             winsText.text = "Wins: 0";
         }
     }   */

    public async Task<int> LoadPlayerStats(string userId)
    {
        var db = FirebaseManager.Instance.Firestore;
        DocumentReference doc = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            int wins = 0;

            if (snap.Exists)
            {
                // Check if stats map exists
                Dictionary<string, object> statsMap;
                if (snap.TryGetValue("stats", out statsMap))
                {
                    if (statsMap.TryGetValue("wins", out object w))
                        wins = Convert.ToInt32(w);
                }
            }

            winsText.text = $"Wins: {wins}";
            return wins;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get wins for user {userId}: {e}");
            winsText.text = "Wins: 0";
            return 0;
        }
    }
}
