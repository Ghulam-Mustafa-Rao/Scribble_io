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
