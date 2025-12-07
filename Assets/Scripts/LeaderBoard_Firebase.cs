using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;

public class LeaderBoard_Firebase : MonoBehaviour
{
    [Header("UI")]
    public Transform leaderboardParent;   // Parent panel for leaderboard rows
    public LeaderboardItem rowPrefab;           // Prefab with: Rank, Email, Score Texts

    private FirebaseFirestore db;
    private FirebaseUser user;

    async void Start()
    {
        user = FirebaseManager.Instance.User;
        db = FirebaseManager.Instance.Firestore;

        await LoadLeaderboard();
    }

    // ----------------------------------------------------------
    // LOAD LEADERBOARD
    // ----------------------------------------------------------
    public async Task LoadLeaderboard()
    {
        Query usersQuery = db.Collection("users");
        QuerySnapshot snap = await usersQuery.GetSnapshotAsync();

        List<PlayerEntry> list = new List<PlayerEntry>();

        foreach (DocumentSnapshot doc in snap.Documents)
        {
            try
            {
                string email =
                    doc.TryGetValue("email", out object emailObj)
                    ? emailObj.ToString()
                    : "Unknown";

                // stats map
                Dictionary<string, object> stats;
                int wins = 0;

                if (doc.TryGetValue("stats", out stats))
                {
                    if (stats.TryGetValue("wins", out object scoreObj))
                        wins = Convert.ToInt32(scoreObj);
                }

                list.Add(new PlayerEntry
                {
                    Email = email,
                    Wins = wins
                });
            }
            catch (Exception e)
            {
                Debug.LogError("Error reading player: " + e);
            }
        }

        // Sort high ? low
        list.Sort((a, b) => b.Wins.CompareTo(a.Wins));

        DisplayLeaderboard(list);
    }

    // ----------------------------------------------------------
    // DISPLAY LEADERBOARD UI
    // ----------------------------------------------------------
    private void DisplayLeaderboard(List<PlayerEntry> list)
    {
        // Clear old rows
        foreach (Transform child in leaderboardParent)
            Destroy(child.gameObject);

        int rank = 1;

        foreach (var entry in list)
        {
            LeaderboardItem row = Instantiate(rowPrefab, leaderboardParent);

            string nickName = entry.Email.Split('@')[0];
            Debug.Log("Before @ (Split): " + nickName);
            // Assuming order: Rank, Email, Score

            row.playerRankText.text = rank.ToString();
            row.playerNameText.text = nickName;
            row.playerScoreText.text = entry.Wins.ToString();

            rank++;
        }
    }

    // ----------------------------------------------------------
    // OPTIONAL: LOAD CURRENT USER SCORE
    // ----------------------------------------------------------
    public async Task<int> LoadPlayerStats(string userId)
    {
        var doc = db.Collection("users").Document(userId);

        try
        {
            var snap = await doc.GetSnapshotAsync();
            int score = 0;

            if (snap.Exists)
            {
                if (snap.TryGetValue("stats", out Dictionary<string, object> stats))
                {
                    if (stats.TryGetValue("scores", out object s))
                        score = Convert.ToInt32(s);
                }
            }

            return score;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get score for user {userId}: {e}");
            return 0;
        }
    }
}

// ----------------------------------------------------------
// HELPER CLASS
// ----------------------------------------------------------
[Serializable]
public class PlayerEntry
{
    public string Email;
    public int Wins;
}
