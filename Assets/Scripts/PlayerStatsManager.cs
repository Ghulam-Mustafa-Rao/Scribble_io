using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using TMPro;
using System;
using System.Collections.Generic;

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

        LoadPlayerStats(FirebaseManager.Instance.User.UserId);
    }


    public async void LoadPlayerStats(string userId)
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
                // Try to get 'wins' field
                int wins = 0;
                if (snap.TryGetValue("wins", out wins))
                {
                    winsText.text = $"Wins: {wins}";
                }
                else
                {
                    winsText.text = "Wins: 0";
                }
            }
            else
            {
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
    }   
}
