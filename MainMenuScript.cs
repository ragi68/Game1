using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuScript : MonoBehaviour
{
    public Animator transition;
    public GameObject MainMenu;
    public GameObject SettingsMenu;
    public GameObject background;

    public void PlayGame(){
        transition.SetTrigger("StartFade");
        MainMenu.SetActive(false);
        SettingsMenu.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitGame(){
        Debug.Log("QUIT");
        Application.Quit();
    }


}
