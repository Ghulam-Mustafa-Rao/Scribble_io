
using UnityEngine;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FirebaseAuthHandler : MonoBehaviour
{
    public LoginScreen loginScreen;
    // ------------------------ REGISTER ------------------------
    public async void RegisterUser(string email, string password)
    {
        if (loginScreen != null)
            loginScreen.register_statusText.text = "Registering...";

        try
        {
            var auth = FirebaseManager.Instance.Auth;
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
          
            FirebaseManager.Instance.User = result.User;
           
            await CreateUserStats(FirebaseManager.Instance.User.UserId);
            if (loginScreen != null)
                loginScreen.register_statusText.text = "Registered successfully!";

            LoginUser(email, password);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            if (loginScreen != null)
                loginScreen.register_statusText.text = $"Error: {e.Message}";
        }
    }

    async Task CreateUserStats(string uid)
    {
        /* var db = FirebaseManager.Instance.Firestore;
         Debug.Log("Step 0: CreateUserStats started");

         // Reference to users/{uid}/stats/main
         DocumentReference docRef = db.Collection("users")
                                      .Document(uid)
                                      .Collection("stats")
                                      .Document("main");

         Debug.Log("Step 1: Document reference created");

         try
         {
             // Use anonymous object with explicit integer value 0
             await docRef.SetAsync(new { wins = 0 });
             Debug.Log("Step 2: User stats created successfully with wins = 0");
         }
         catch (System.Exception e)
         {
             Debug.LogError("Failed to create user stats: " + e);
         }*/

        var db = FirebaseManager.Instance.Firestore;
        Debug.Log("Step 0: CreateUserStats started");

        // Reference to users/{uid} document
        DocumentReference docRef = db.Collection("users").Document(uid);
        Debug.Log("Step 1: Document reference created");

        try
        {
            // Store stats as a map inside the user document
            var data = new Dictionary<string, object>
            {
                { "stats", new Dictionary<string, object> { { "wins", 0 } } }
            };

            await docRef.SetAsync(data, SetOptions.MergeAll); // merge to preserve other fields
            Debug.Log("Step 2: User stats created successfully with wins = 0");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to create user stats: " + e);
        }
    }
   
    // ------------------------ LOGIN ------------------------
    public async void LoginUser(string email, string password)
    {
        if (loginScreen != null)
        {
            loginScreen.login_statusText.text = "Logging in...";
            loginScreen.register_statusText.text = "Logging in...";
        }

        try
        {
            var auth = FirebaseManager.Instance.Auth;
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);

            FirebaseManager.Instance.User = result.User;

            if (loginScreen != null)
            {
                loginScreen.login_statusText.text = "Login success!";
                loginScreen.register_statusText.text = "Login success!";
            }
        
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }
        catch (Firebase.FirebaseException e)
        {
            string message;
            var authError = (Firebase.Auth.AuthError)(int)e.ErrorCode;
            if (authError == Firebase.Auth.AuthError.Failure)
            {
                // Generic error: assume wrong credentials if login failed
                message = "Login failed: Invalid email or password.";
            }
            else
            {
                switch (authError)
                {
                    case Firebase.Auth.AuthError.InvalidEmail:
                        message = "Login failed: Invalid email address.";
                        break;
                    case Firebase.Auth.AuthError.WrongPassword:
                        message = "Login failed: Wrong password.";
                        break;
                    case Firebase.Auth.AuthError.UserNotFound:
                        message = "Login failed: User not found.";
                        break;
                    case Firebase.Auth.AuthError.UserDisabled:
                        message = "Login failed: User account disabled.";
                        break;
                    case Firebase.Auth.AuthError.EmailAlreadyInUse:
                        message = "Login failed: Email already in use.";
                        break;
                    default:
                        message = $"Login failed: {e.Message} | AuthError: {authError}";
                        break;
                }
            }

            Debug.LogError(message);
            loginScreen.login_statusText.text = message;
            loginScreen.register_statusText.text = message;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to login: " + e);
            if (loginScreen != null)
            {
                loginScreen.login_statusText.text = $"Error: {e.Message}";
                loginScreen.register_statusText.text = $"Error: {e.Message}";
            }
        }
    }

}
