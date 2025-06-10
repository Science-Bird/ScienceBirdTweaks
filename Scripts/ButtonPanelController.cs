using System.Collections;
using BepInEx;
using ScienceBirdTweaks.Patches;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ScienceBirdTweaks.Scripts
{
    public class ButtonPanelController : NetworkBehaviour
    {
        public Animator panelAnimator;
        public InteractTrigger redButton1;
        public InteractTrigger redButton2;
        public InteractTrigger blackButton1;
        public InteractTrigger blackButton2;
        public InteractTrigger smallGreenButton;
        public InteractTrigger smallRedButton;
        public InteractTrigger knob1;
        public InteractTrigger knob2;
        public InteractTrigger knob3;
        public InteractTrigger smallKnob;

        public static ShipFloodlightController floodlightController;
        public ShipFloodlightInteractionHandler floodlightInteractHandler;

        public string redStr1;
        public string redStr2;
        public string blackStr1;
        public string blackStr2;
        public string smallRedStr;
        public string smallGreenStr;
        public string knobStr1;
        public string knobStr2;
        public string knobStr3;
        public string smallKnobStr;


        public AudioClip knobTurnSFX;

        private int knob1State;
        private int knob2State;
        private int knob3State;
        private int smallKnobState;
        private bool smallRedState = false;
        private bool smallGreenState = false;

        public bool fancyPanel = false;
        public bool floodlight = false;
        public bool floodlightExtra = false;
        public bool floodlightTarget = false;

        private bool sendingRPC;

        public static ButtonPanelController Instance { get; set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                UnityEngine.Object.Destroy(Instance.gameObject);
            }
        }

        private void Start()
        {
            fancyPanel = ScienceBirdTweaks.FancyPanel.Value;
            floodlight = ScienceBirdTweaks.FloodlightRotation.Value;
            floodlightExtra = ScienceBirdTweaks.FloodlightExtraControls.Value;
            floodlightTarget = ScienceBirdTweaks.FloodlightPlayerFollow.Value;

            if (floodlight)
            {
                floodlightController = Object.FindObjectOfType<ShipFloodlightController>();
            }
            if (fancyPanel)
            {
                SetupStrings();
                SetupLights("all");
            }
            if (floodlight)
            {
                SetupLights("floodlight");
                if (floodlightExtra)
                {
                    SetupLights("floodlightExtra");
                    if (!floodlightTarget)
                    {
                        blackButton2.hoverTip = "";
                        if (!fancyPanel)
                        {
                            blackButton2.interactable = false;
                        }
                    }
                }
            }
        }

        public void SetupStrings()// you can patch this to change tooltips
        {
            redStr1 = ScienceBirdTweaks.Red1Tip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.Red1Tip.Value + " : [LMB]";
            redStr2 = ScienceBirdTweaks.Red2Tip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.Red2Tip.Value + " : [LMB]"; ;
            blackStr1 = ScienceBirdTweaks.Black1Tip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.Black1Tip.Value + " : [LMB]"; ;
            blackStr2 = ScienceBirdTweaks.Black2Tip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.Black2Tip.Value + " : [LMB]"; ;
            smallRedStr = ScienceBirdTweaks.SmallRedTip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.SmallRedTip.Value + " : [LMB]"; ;
            smallGreenStr = ScienceBirdTweaks.SmallGreenTip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.SmallGreenTip.Value + " : [LMB]"; ;
            knobStr1 = ScienceBirdTweaks.Knob1Tip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.Knob1Tip.Value + " : [LMB]"; ;
            knobStr2 = ScienceBirdTweaks.Knob2Tip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.Knob2Tip.Value + " : [LMB]"; ;
            knobStr3 = ScienceBirdTweaks.Knob3Tip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.Knob3Tip.Value + " : [LMB]"; ;
            smallKnobStr = ScienceBirdTweaks.SmallKnobTip.Value.IsNullOrWhiteSpace() ? "" : ScienceBirdTweaks.SmallKnobTip.Value + " : [LMB]"; ;
        }

        public void SetupLights(string mode = "all")
        {
            switch (mode)
            {
                case "all":
                    redButton1.interactable = true;
                    redButton1.hoverTip = redStr1;
                    redButton2.interactable = true;
                    redButton2.hoverTip = redStr2;
                    blackButton1.interactable = true;
                    blackButton1.hoverTip= blackStr1;
                    blackButton2.interactable = true;
                    blackButton2.hoverTip = blackStr2;
                    smallGreenButton.interactable = true;
                    smallGreenButton.hoverTip = smallGreenStr;
                    smallRedButton.interactable = true;
                    smallRedButton.hoverTip = smallRedStr;
                    knob1.interactable = true;
                    knob1.hoverTip = knobStr1;
                    knob2.interactable = true;
                    knob2.hoverTip = knobStr2;
                    knob3.interactable = true;
                    knob3.hoverTip = knobStr3;
                    smallKnob.interactable = true;
                    smallKnob.hoverTip = smallKnobStr;
                    break;
                case "floodlight":
                    redButton1.interactable = true;
                    redButton1.hoverTip = "Toggle light rotation : [LMB]";
                    break;
                case "floodlightExtra":
                    blackButton1.interactable = true;
                    blackButton1.hoverTip = "Reset light rotation : [LMB]";
                    blackButton2.interactable = true;
                    blackButton2.hoverTip = "Toggle player targeting : [LMB]";
                    knob1.interactable = true;
                    knob1.hoverTip = "Rotation speed (1.0x) : [LMB]";
                    SetKnob1(2);// default knob state is 90 degrees turned
                    break;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PanelAnimationServerRpc(int num, int player)
        {
            PanelAnimationClientRpc(num, player);
        }

        [ClientRpc]
        public void PanelAnimationClientRpc(int num, int player)
        {
            if (sendingRPC)
            {
                sendingRPC = false;
            }
            else
            {
                PanelAnimation(num, player);
            }

        }

        public void PanelAnimationLocal(int num)// this is the function every button and knob on the panel calls
        {
            sendingRPC = true;
            PanelAnimation(num, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
            PanelAnimationServerRpc(num, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        }

        public void PanelAnimation(int num, int player)
        {
            if (floodlight && (num == 0 || (num == 1 && floodlightExtra) || (num == 3 && floodlightExtra && floodlightTarget) || (num == 4 && floodlightExtra)) && floodlightController == null)
            {
                floodlightController = Object.FindObjectOfType<ShipFloodlightController>();
                if (floodlightController == null)
                {
                    ShipFloodlightsPatch.AddSpinnerComponentPatch(StartOfRound.Instance);
                }
                if (floodlightController == null)
                {
                    return;
                }
            }
            switch (num)// for each interaction: if it has an assigned purpose, use that, otherwise fall back to generic function
            {
                case 0:
                    if (floodlight)
                    {
                        floodlightInteractHandler.ToggleSpinning();
                    }
                    else
                    {
                        GreenLightSet(!panelAnimator.GetBool("GreenOn"));// toggle light
                        Red1Func(player, panelAnimator.GetBool("GreenOn"));
                    }
                    break;
                case 1:
                    if (floodlight && floodlightExtra)
                    {
                        floodlightController.ResetRotation();
                    }
                    else
                    {
                        Black1Func(player);
                    }
                    break;
                case 2:
                    Red2Func(player);
                    break;
                case 3:
                    if (floodlight && floodlightExtra && floodlightTarget)
                    {
                        floodlightController.SetTargeting();
                    }
                    else
                    {
                        BlueLightSet(!panelAnimator.GetBool("BlueOn"));
                        Black2Func(player, panelAnimator.GetBool("BlueOn"));
                    }
                    break;
                case 4:
                    TurnKnob1();
                    if (floodlight && floodlightExtra)
                    {
                        floodlightController.SetRotationSpeed(-1f, knob1State);
                    }
                    else
                    {
                        Knob1Func(player, knob1State);
                    }
                    break;
                case 5:
                    TurnKnob2();
                    Knob2Func(player, knob2State);
                    break;
                case 6:
                    TurnKnob3();
                    Knob3Func(player, knob3State);
                    break;
                case 7:
                    TurnSmallKnob();
                    SmallKnobFunc(player, smallKnobState);
                    break;
                case 8:
                    SmallRedLightSet(!panelAnimator.GetBool("SmallRedOn"));
                    SmallRedFunc(player, panelAnimator.GetBool("SmallRedOn"));
                    break;
                case 9:
                    SmallGreenLightSet(!panelAnimator.GetBool("SmallGreenOn"));
                    SmallGreenFunc(player, panelAnimator.GetBool("SmallGreenOn"));
                    break;
            }
        }

        public void SetLightAfterDelay(int light, float time, bool on)
        {
            StartCoroutine(LightDelay(light, time, on));
        }

        private IEnumerator LightDelay(int light, float time, bool on)
        {
            yield return new WaitForSeconds(time);
            switch (light)
            {
                case 0:
                    BlueLight1Set(on);
                    break;
                case 1:
                    BlueLight2Set(on);
                    break;
                case 2:
                    GreenLight1Set(on);
                    break;
                case 3:
                    GreenLight2Set(on);
                    break;
                case 4:
                    GreenLight3Set(on);
                    break;
                case 5:
                    OrangeRoundSet(on);
                    break;
                case 6:
                    OrangeTallSet(on);
                    break;
                case 7:
                    RedLightSet(on);
                    break;
            }
        }

        private void PlayKnobSFX(int knob)
        {
            switch (knob)
            {
                case 0:
                    knob1.gameObject.GetComponent<AudioSource>().PlayOneShot(knobTurnSFX);
                    break;
                case 1:
                    knob2.gameObject.GetComponent<AudioSource>().PlayOneShot(knobTurnSFX);
                    break;
                case 2:
                    knob3.gameObject.GetComponent<AudioSource>().PlayOneShot(knobTurnSFX);
                    break;
                case 3:
                    smallKnob.gameObject.GetComponent<AudioSource>().PlayOneShot(knobTurnSFX);
                    break;
            }
        }

        public void BlueLightSet(bool on)
        {
            panelAnimator.SetBool("BlueOn", on);
        }

        public void GreenLightSet(bool on)
        {
            panelAnimator.SetBool("GreenOn", on);
        }

        public void RedLightSet(bool on)
        {
            panelAnimator.SetBool("BaseRedOn", on);
        }

        public void BlueLight1Set(bool on)
        {
            panelAnimator.SetBool("BaseBlue1On", on);
        }

        public void BlueLight2Set(bool on)
        {
            panelAnimator.SetBool("BaseBlue2On", on);
        }

        public void GreenLight1Set(bool on)
        {
            panelAnimator.SetBool("BaseGreen1On", on);
        }

        public void GreenLight2Set(bool on)
        {
            panelAnimator.SetBool("BaseGreen2On", on);
        }

        public void GreenLight3Set(bool on)
        {
            panelAnimator.SetBool("BaseGreen3On", on);
        }

        public void OrangeRoundSet(bool on)
        {
            panelAnimator.SetBool("BaseOrangeRoundOn", on);
        }

        public void OrangeTallSet(bool on)
        {
            panelAnimator.SetBool("BaseOrangeTallOn", on);
        }

        public void SmallGreenLightSet(bool on)
        {
            panelAnimator.SetBool("SmallGreenOn", on);
        }

        public void SmallRedLightSet(bool on)
        {
            panelAnimator.SetBool("SmallRedOn", on);
        }

        public void Knob1Tip(string tip)
        {
            knob1.hoverTip = tip;
        }
        public void TurnKnob1()
        {
            knob1State++;
            knob1State = knob1State % 8;
            panelAnimator.SetInteger("KnobA", knob1State);
            PlayKnobSFX(0);
        }
        public void SetKnob1(int state)
        {
            knob1State = state;
            knob1State = knob1State % 8;
            panelAnimator.SetInteger("KnobA", knob1State);
            PlayKnobSFX(0);
        }

        public void Knob2Tip(string tip)
        {
            knob2.hoverTip = tip;
        }
        public void TurnKnob2()
        {
            knob2State++;
            knob2State = knob2State % 8;
            panelAnimator.SetInteger("KnobB", knob2State);
            PlayKnobSFX(1);
        }
        public void SetKnob2(int state)
        {
            knob2State = state;
            knob2State = knob2State % 8;
            panelAnimator.SetInteger("KnobB", knob2State);
            PlayKnobSFX(1);
        }

        public void Knob3Tip(string tip)
        {
            knob3.hoverTip = tip;
        }
        public void TurnKnob3()
        {
            knob3State++;
            knob3State = knob3State % 8;
            panelAnimator.SetInteger("KnobC", knob3State);
            PlayKnobSFX(2);
        }
        public void SetKnob3(int state)
        {
            knob3State = state;
            knob3State = knob3State % 8;
            panelAnimator.SetInteger("KnobC", knob3State);
            PlayKnobSFX(2);
        }

        public void SmallKnobTip(string tip)
        {
            smallKnob.hoverTip = tip;
        }
        public void TurnSmallKnob()
        {
            smallKnobState++;
            smallKnobState = smallKnobState % 8;
            panelAnimator.SetInteger("SmallKnob", smallKnobState);
            PlayKnobSFX(3);
        }
        public void SetSmallKnob(int state)
        {
            smallKnobState = state;
            smallKnobState = smallKnobState % 8;
            panelAnimator.SetInteger("SmallKnob", smallKnobState);
            PlayKnobSFX(3);
        }

        // you can patch these to do whatever you want! they all provide the id of the player using them, knobs tell you their rotation state (0-7), and buttons which control lights tell you the state of the light
        public void Knob1Func(int player, int state)
        {
        }
        public void Knob2Func(int player, int state)
        {
        }
        public void Knob3Func(int player, int state)
        {
        }
        public void SmallKnobFunc(int player, int state)
        {
        }
        public void Red1Func(int player, bool on)
        {
        }
        public void Red2Func(int player)
        {
        }
        public void Black1Func(int player)
        {
        }
        public void Black2Func(int player, bool on)
        {
        }
        public void SmallRedFunc(int player, bool on)
        {
        }
        public void SmallGreenFunc(int player, bool on)
        {
        }

    }
}