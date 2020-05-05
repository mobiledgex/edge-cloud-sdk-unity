using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class ExampleScene : MonoBehaviour
{
    #region public Variables assigned in the inspector
    public TextMesh StatusText;
    public Animator StatusAnimator;
    public Animator LoadingBall;
    public Animator CameraAniamtor;
    public Animator PlaneAnimator;
    public Animator PortAnimator;

    public Button nextButton;
    public Text infoText;
    #endregion
    // for scene progression
    static int step;
    #region MonoBehaviour Callbacks
    private void Awake()
    {
        nextButton.onClick.AddListener(IncrementStep);
    }

    private void OnEnable()
    {
        StatusText.text = "";
        StartCoroutine(SceneFlow());
    }
    #endregion

    #region Scene Functions

    void IncrementStep()
    {
        step += 1;
        if (StatusAnimator.enabled)
        {
            StatusAnimator.SetInteger("Step", step);
        }

        if (PortAnimator.enabled)
        {
            PortAnimator.SetInteger("Step", step);
        }

        if (CameraAniamtor.enabled)
        {
            CameraAniamtor.SetInteger("Step", step);
        }

        if (PlaneAnimator.enabled)
        {
            PlaneAnimator.SetInteger("Step", step);
        }

        if (infoText.enabled)
        {
            infoText.text = InfoText(step);
        }


    }

    string InfoText(int step)
    {

        switch (step)
        {
            case 0:
                infoText.transform.parent.gameObject.SetActive(true);
                return infoText.text = "<b>RegisterClient()</b> \n Verfies the app has been deployed to MobiledgeX Cloudlets using (Organization Name, AppName &AppVersion).";

            case 2:
                infoText.transform.parent.gameObject.SetActive(true);
                return infoText.text = "<b>FindCloudlet() </b> \n Gets the address of the best cloudlet that is running your server application.";

            case 4:
                infoText.transform.parent.gameObject.SetActive(true);
                return infoText.text = "<b>GetConnection based on Protocol </b> \n Provides reliable optimized connection between the cloudlet and your client application based on the protocol selected";
            default:
                infoText.transform.parent.gameObject.SetActive(false);
                return "";
        }
    }

    IEnumerator SceneFlow()
    {

        StatusText.text = "Register Client";
        infoText.text = InfoText(step);
        yield return new WaitForSeconds(1);

        while (StatusAnimator.GetInteger("Step") != 1)
        {
            yield return null;
        }
        nextButton.interactable = false;
        StatusAnimator.SetTrigger("Start");
        yield return new WaitForSeconds(1);
        StatusText.text = "Verified";
        StatusAnimator.SetTrigger("End");
        yield return new WaitForSeconds(2);
        nextButton.interactable = true;
        while (StatusAnimator.GetInteger("Step") != 2)
        {
            yield return null;
        }
        nextButton.interactable = false;
        StatusAnimator.SetTrigger("Reset");
        StatusText.text = "";
        yield return new WaitForSeconds(1.5f);
        StatusText.text = "Find Cloudlet";
        yield return new WaitForSeconds(1);
        StatusAnimator.SetTrigger("Start");
        yield return new WaitForSeconds(1);
        CameraAniamtor.SetTrigger("CameraMove");
        yield return new WaitForSeconds(1);
        PlaneAnimator.enabled = true;
        nextButton.interactable = true;
        while (StatusAnimator.GetInteger("Step") != 3)
        {
            yield return null;
        }
        nextButton.interactable = false;
        yield return new WaitForSeconds(1f);
        CameraAniamtor.SetTrigger("CameraBack");
        yield return new WaitForSeconds(1);
        PlaneAnimator.gameObject.SetActive(false);
        StatusText.text = "Cloudlet Found";
        StatusAnimator.SetTrigger("End");
        yield return new WaitForSeconds(2f);
        StatusAnimator.SetTrigger("Reset");
        StatusText.text = "";
        yield return new WaitForSeconds(1.5f);
        StatusText.text = "Connecting to \n Desired Ports";
        nextButton.interactable = true;
        while (StatusAnimator.GetInteger("Step") != 4)
        {
            yield return null;
        }
        nextButton.interactable = false;
        yield return new WaitForSeconds(1.5f);
        StatusAnimator.SetTrigger("Start");
        CameraAniamtor.SetTrigger("CameraMove");
        PortAnimator.gameObject.SetActive(true);
        yield return new WaitForSeconds(1);
        PortAnimator.enabled = true;
        yield return new WaitForSeconds(1.5f);
        nextButton.interactable = true;
        while (StatusAnimator.GetInteger("Step") != 5)
        {
            yield return null;
        }
        nextButton.interactable = false;
        CameraAniamtor.SetTrigger("CameraBack");
        yield return new WaitForSeconds(2f);
        StatusText.text = "Port Found";
        StatusAnimator.SetTrigger("End");
        yield return new WaitForSeconds(1.5f);
        StatusAnimator.SetTrigger("Reset");
        yield return new WaitForSeconds(1);
        StatusText.text = "Connected";
        LoadingBall.enabled = true;
    }
    #endregion
}
