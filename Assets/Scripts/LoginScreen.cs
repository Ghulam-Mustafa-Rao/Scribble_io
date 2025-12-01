
using TMPro;
using UnityEngine;

public class LoginScreen : MonoBehaviour
{
    [Header("Login UI")]
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;
    private bool isVisible_login;
    public TextMeshProUGUI login_statusText;

    [Header("Register UI")]
    public TMP_InputField regEmail;
    public TMP_InputField regPassword;
    private bool isVisible_reg;
    public TextMeshProUGUI register_statusText;

    [Header("Other")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [SerializeField]
    FirebaseAuthHandler firebaseAuthHandler;

    private void Start()
    {
        firebaseAuthHandler.loginScreen = this;
        OpenLoginPanel();
    }

    // ------------------------ REGISTER ------------------------
    public void RegisterUser()
    {
        string email = regEmail.text;
        string password = regPassword.text;
        firebaseAuthHandler.RegisterUser(email, password);
    }


    // ------------------------ LOGIN ------------------------
    public void LoginUser()
    {
        string email = loginEmail.text;
        string password = loginPassword.text;
        firebaseAuthHandler.LoginUser(email, password);
    }

    // ------------------------ UI NAVIGATION ------------------------
    public void OpenRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        register_statusText.text = "";
    }

    public void OpenLoginPanel()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
        login_statusText.text = "";
    }

    public void ToggleLoginPassword()
    {
        isVisible_login = !isVisible_login;
        loginPassword.contentType = isVisible_login ?
            TMP_InputField.ContentType.Standard :            // Show text
            TMP_InputField.ContentType.Password;             // Hide text

        loginPassword.ForceLabelUpdate();
    }

    public void ToggleRegisterPassword()
    {
        isVisible_reg = !isVisible_reg;
        regPassword.contentType = isVisible_reg ?
            TMP_InputField.ContentType.Standard :            // Show text
            TMP_InputField.ContentType.Password;             // Hide text

        regPassword.ForceLabelUpdate();
    }

}
