using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;
using System.Text.RegularExpressions;

public class cookieJarsScript : MonoBehaviour
{

    public KMBombModule Module;
    public KMBossModule Boss;
    public KMAudio Audio;
    public KMBombInfo Info;
    public KMSelectable jar, left, right;
    public TextMesh cookieAmountText, jarText;
    public MeshRenderer[] leds;
    public Material unlit, lit;
    public Transform jarTransform;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool solved;

    private readonly int[] cookies = { 99, 99, 99 };
    private readonly string[] cookieNames = { "chocolate\nchip cookies", "sugar\ncookies", "m&m\ncookies", "oatmeal\nraisin\ncookies", "snickerdoodles", "peanut\nbutter\ncookies", "fortune\ncookies",
                                              "butter\ncookies", "gingerbread\ncookies", "OREOs" };
    private readonly string[] cookieNamesLog = { "chocolate chip", "sugar", "m&m", "oatmeal raisin", "snickerdoodle", "peanut butter", "fortune", "butter", "gingerbread", "OREO" };
    private int shownJar = 0;
    private readonly int[] cookieAmounts = { 0, 0, 0 };
    private int lastEaten, lastLastEaten;
    private int hunger = 0;

    private readonly bool[] correctBtns = { false, false, false };
    private int highestCookie = 0, secondHighestCookie = 0, lowestCookie;

    int solves = 0;
    private string[] ignoredModules;
    private bool tpCorrect = false;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        jar.OnInteract += delegate ()
        {
            if (!solved)
                EatCookie();
            jar.AddInteractionPunch();
            return false;
        };

        left.OnInteract += delegate ()
        {
            if (!solved)
                ChangeJar(-1);
            left.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Module.transform);
            return false;
        };

        right.OnInteract += delegate ()
        {
            if (!solved)
                ChangeJar(1);
            right.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Module.transform);
            return false;
        };

        for (int i = 0; i < 5; i++)
            leds[i].material = unlit;
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return null;
        if (ignoredModules == null)
            ignoredModules = Boss.GetIgnoredModules("Cookie Jars", new string[] { "Cookie Jars" });
        GenerateModule();
    }

    void EatCookie()
    {
        var allSolved = Info.GetSolvedModuleNames();
        var solvedModules = allSolved.Except(ignoredModules).Count();
        var solvableModules = Info.GetSolvableModuleNames().Except(ignoredModules).Count();

        if ((solvedModules == solvableModules || hunger != 0) && cookieAmounts[shownJar] > 0)
        {
            CheckCookies(allSolved.Count);

            if (correctBtns[shownJar])
            {
                lastLastEaten = lastEaten;
                lastEaten = cookies[shownJar];
                tpCorrect = true;
                Debug.LogFormat("[Cookie Jars #{0}] You ate a{2} {1} cookie. That was right!", _moduleId, cookieNamesLog[cookies[shownJar]], cookieNamesLog[cookies[shownJar]] == "OREO" ? "n" : "");
                cookieAmounts[shownJar]--;
                hunger = 0;
                Audio.PlaySoundAtTransform("OmNom", Module.transform);

                for (int i = 0; i < 5; i++)
                    leds[i].material = unlit;

                if (cookieAmounts[shownJar] == 0)
                    cookieAmountText.text = "[ No cookies! :( ]";

                else if (cookieAmounts[shownJar] == 1)
                    cookieAmountText.text = "[ 1 cookie! :| ]";

                else
                    cookieAmountText.text = "[ " + cookieAmounts[shownJar] + " cookies! :) ]";

                if (cookieAmounts[0] == 0 && cookieAmounts[1] == 0 && cookieAmounts[2] == 0)
                {
                    Module.HandlePass();

                    Debug.LogFormat("[Cookie Jars #{0}] You ate all the cookies and solved the module!", _moduleId);

                    jarText.text = "GG!";
                    cookieAmountText.text = "[ No cookies!!! D: ]";

                    for (int i = 0; i < 5; i++)
                        leds[i].material = lit;

                    solved = true;
                }
            }

            else
            {
                Debug.LogFormat("[Cookie Jars #{0}] You ate a{2} {1} cookie. You got food poisoning and died! STRIKE!", _moduleId, cookieNamesLog[cookies[shownJar]], cookieNamesLog[cookies[shownJar]] == "OREO" ? "n" : "");
                hunger = 0;

                Module.HandleStrike();
                Audio.PlaySoundAtTransform("OhNo", Module.transform);
            }
        }
    }

    void ChangeJar(int changeNum)
    {
        shownJar = (((shownJar + changeNum) % 3) + 3) % 3;
        StartCoroutine("Spin", changeNum);
        if (cookieAmounts[shownJar] == 0)
            cookieAmountText.text = "[ No cookies! :( ]";
        else if (cookieAmounts[shownJar] == 1)
            cookieAmountText.text = "[ 1 cookie! :| ]";
        else
            cookieAmountText.text = "[ " + cookieAmounts[shownJar] + " cookies! :) ]";
    }

    void GenerateModule()
    {
        for (int i = 0; i < cookies.Length; i++)
        {
            int rndCookie = Random.Range(0, 10);

            while (cookies.Contains(rndCookie))
            {
                rndCookie = (rndCookie + 1) % 10;
            }

            cookies[i] = rndCookie;
        }

        for (int i = 0; i < cookies.Length; i++)
        {
            if (cookies[i] < cookies[(i + 1) % 3] && cookies[i] < cookies[(i + 2) % 3])
            {
                highestCookie = i;
                Debug.LogFormat("[Cookie Jars #{0}] The {1} are the highest on the list.", _moduleId, cookieNamesLog[cookies[i]]);
            }

            else if (cookies[i] < cookies[(i + 1) % 3] || cookies[i] < cookies[(i + 2) % 3])
            {
                secondHighestCookie = i;
                Debug.LogFormat("[Cookie Jars #{0}] The {1} are the second highest on the list.", _moduleId, cookieNamesLog[cookies[i]]);
            }

            else
            {
                lowestCookie = i;
                Debug.LogFormat("[Cookie Jars #{0}] The {1} are the lowest on the list.", _moduleId, cookieNamesLog[cookies[i]]);
            }
        }

        float averageCookies = Info.GetSolvableModuleNames().Count / 10f;
        int slightlyLessAccurateAverageCookies = Info.GetSolvableModuleNames().Count / 10;

        if (slightlyLessAccurateAverageCookies - averageCookies < .3f)
        {
            cookieAmounts[0] = slightlyLessAccurateAverageCookies;
            cookieAmounts[1] = slightlyLessAccurateAverageCookies;
            cookieAmounts[2] = slightlyLessAccurateAverageCookies;
        }

        else if (slightlyLessAccurateAverageCookies - averageCookies < .6f)
        {
            cookieAmounts[0] = slightlyLessAccurateAverageCookies + 1;
            cookieAmounts[1] = slightlyLessAccurateAverageCookies;
            cookieAmounts[2] = slightlyLessAccurateAverageCookies;
        }

        else
        {
            cookieAmounts[0] = slightlyLessAccurateAverageCookies + 1;
            cookieAmounts[1] = slightlyLessAccurateAverageCookies + 1;
            cookieAmounts[2] = slightlyLessAccurateAverageCookies;
        }

        // *starts 967 module bomb*
        // *gets 290 cookies* 
        // *https://i.kym-cdn.com/entries/icons/original/000/027/475/Screen_Shot_2018-10-25_at_11.02.15_AM.png*

        if (cookieAmounts[2] == 0)
        {
            cookieAmounts[0] = 1;
            cookieAmounts[1] = 1;
            cookieAmounts[2] = 1;
        }

        jarText.text = cookieNames[cookies[shownJar]];

        if (cookieAmounts[shownJar] == 0)
            cookieAmountText.text = "[ No cookies! :( ]";
        else if (cookieAmounts[shownJar] == 1)
            cookieAmountText.text = "[ 1 cookie! :| ]";
        else
            cookieAmountText.text = "[ " + cookieAmounts[shownJar] + " cookies! :) ]";

        lastEaten = Info.GetSerialNumberNumbers().First();
        lastLastEaten = Info.GetSerialNumberNumbers().Skip(1).First();

        for (int i = 0; i < 3; i++)
            Debug.LogFormat("[Cookie Jars #{0}] One of the jars has {1} {2} inside.", _moduleId, cookieAmounts[i], cookieNames[cookies[i]].Replace("\n", " "));

        Debug.LogFormat("[Cookie Jars #{0}] The last eaten cookie was a{3} {1} cookie and the cookie eaten before that was a{4} {2} cookie.", _moduleId, cookieNamesLog[lastEaten], cookieNamesLog[lastLastEaten], cookieNamesLog[lastEaten] == "OREO" ? "n" : "", cookieNamesLog[lastLastEaten] == "OREO" ? "n" : "");
    }

    void CheckCookies(int numSolved)
    {
        for (int i = 0; i < 3; i++)
        {
            correctBtns[i] = false;
            if (cookieAmounts[i] > 0)
                if (cookies[i] == 0 && lastEaten != lastLastEaten || cookies[i] == 1 && lastEaten == lastLastEaten || cookies[i] == 2 && lastEaten < lastLastEaten || cookies[i] == 3 && lastEaten > lastLastEaten || cookies[i] == 4 && lastEaten == 4 || cookies[i] == 5 && lastEaten != 5 || cookies[i] == 6 && lastEaten % 2 == numSolved % 2 || cookies[i] == 7 && lastEaten % 2 != numSolved % 2 || cookies[i] == 8 && cookieAmounts[i] % 2 == numSolved % 2 || cookies[i] == 9 && cookieAmounts[i] % 2 != numSolved % 2)
                    correctBtns[i] = true;
        }

        if (!correctBtns.Contains(true))
        {
            if (cookieAmounts[highestCookie] > 0)
                correctBtns[highestCookie] = true;
            else if (cookieAmounts[secondHighestCookie] > 0)
                correctBtns[secondHighestCookie] = true;
            else
                correctBtns[lowestCookie] = true;
        }

        for (int i = 0; i < 3; i++)
            Debug.LogFormat("[Cookie Jars #{0}] Can {1} be eaten? {2}", _moduleId, cookieNamesLog[cookies[i]], correctBtns[i] ? "Yes!" : "No!");
    }

    private void Update()
    {
        if (Info.GetSolvedModuleNames().Count > solves && !solved)
        {
            StopCoroutine("StrikeAnimation");
            solves++;
            hunger++;

            for (int i = 0; i < hunger; i++)
                leds[i].material = lit;

            if (hunger == 5)
            {
                Module.HandleStrike();
                Debug.LogFormat("[Cookie Jars #{0}] You didn't eat a cookie. You starved to death! STRIKE!", _moduleId);

                StartCoroutine("HungerAnimation");
            }
        }
    }

    IEnumerator HungerAnimation()
    {
        hunger = 0;
        Audio.PlaySoundAtTransform("OhNo", Module.transform);

        for (int i = 0; i < 20; i++)
        {
            leds[0].material = lit;
            leds[1].material = lit;
            leds[2].material = lit;
            leds[3].material = lit;
            leds[4].material = lit;

            yield return new WaitForSeconds(.05f);

            leds[0].material = unlit;
            leds[1].material = unlit;
            leds[2].material = unlit;
            leds[3].material = unlit;
            leds[4].material = unlit;

            yield return new WaitForSeconds(.05f);
        }

    }

    IEnumerator Spin(int spinDirection)
    {
        for (int i = 0; i < 24; i++)
        {
            jarTransform.transform.Rotate(Vector3.down, 15 * spinDirection);
            yield return new WaitForSeconds(.005f);
            if (i == 12)
                jarText.text = cookieNames[cookies[shownJar]];
        }
    }

    //twitch plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cycle will cycle through the jars. !{0} eat will eat a cookie from the jar. !{0} left/!{0} right move to the left/right jars respectively.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string cmd)
    {
        if (Regex.IsMatch(cmd, @"^\s*cycle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            right.OnInteract();
            yield return new WaitForSeconds(2.5f);
            right.OnInteract();
            yield return new WaitForSeconds(2.5f);
            right.OnInteract();
            yield return new WaitForSeconds(1.0f);
            yield break;
        }
        if (Regex.IsMatch(cmd, @"^\s*eat\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            jar.OnInteract();
            if (tpCorrect)
            {
                yield return "awardpoints 1";
                tpCorrect = false;
            }
            yield break;
        }
        if (Regex.IsMatch(cmd, @"^\s*left\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            left.OnInteract();
            yield break;
        }
        if (Regex.IsMatch(cmd, @"^\s*right\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            right.OnInteract();
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        jarText.text = "GG!";
        cookieAmountText.text = "[ No cookies!!! D: ]";

        leds[0].material = lit;
        leds[1].material = lit;
        leds[2].material = lit;
        leds[3].material = lit;
        leds[4].material = lit;
        solved = true;
    }

    void ForceCookies(int cookie1, int cookie2, int cookie3)
    {
        cookies[0] = cookie1;
        cookies[1] = cookie2;
        cookies[2] = cookie3;

        for (int i = 0; i < cookies.Length; i++)
        {
            if (cookies[i] > cookies[(i + 1) % cookies.Length] && cookies[i] > cookies[(i + 2) % cookies.Length])
                highestCookie = i;
            else if (cookies[i] > cookies[(i + 1) % cookies.Length] || cookies[i] > cookies[(i + 2) % cookies.Length])
                secondHighestCookie = i;
            else
                lowestCookie = i;
        }

        float averageCookies = Info.GetSolvableModuleNames().Count / 10f;
        int slightlyLessAccurateAverageCookies = Info.GetSolvableModuleNames().Count / 10;

        if (slightlyLessAccurateAverageCookies - averageCookies < .3f)
        {
            cookieAmounts[0] = slightlyLessAccurateAverageCookies;
            cookieAmounts[1] = slightlyLessAccurateAverageCookies;
            cookieAmounts[2] = slightlyLessAccurateAverageCookies;
        }

        else if (slightlyLessAccurateAverageCookies - averageCookies < .6f)
        {
            cookieAmounts[0] = slightlyLessAccurateAverageCookies + 1;
            cookieAmounts[1] = slightlyLessAccurateAverageCookies;
            cookieAmounts[2] = slightlyLessAccurateAverageCookies;
        }

        else
        {
            cookieAmounts[0] = slightlyLessAccurateAverageCookies + 1;
            cookieAmounts[1] = slightlyLessAccurateAverageCookies + 1;
            cookieAmounts[2] = slightlyLessAccurateAverageCookies;
        }

        // *starts 967 module bomb*
        // *gets 290 cookies* 
        // *https://i.kym-cdn.com/entries/icons/original/000/027/475/Screen_Shot_2018-10-25_at_11.02.15_AM.png*

        if (cookieAmounts[2] == 0)
        {
            cookieAmounts[0] = 1;
            cookieAmounts[1] = 1;
            cookieAmounts[2] = 1;
        }

        jarText.text = cookieNames[cookies[shownJar]];

        if (cookieAmounts[shownJar] == 0)
            cookieAmountText.text = "[ No cookies! :( ]";
        else if (cookieAmounts[shownJar] == 1)
            cookieAmountText.text = "[ 1 cookie! :| ]";
        else
            cookieAmountText.text = "[ " + cookieAmounts[shownJar] + " cookies! :) ]";
        lastEaten = Info.GetSerialNumberNumbers().First();
        lastLastEaten = Info.GetSerialNumberNumbers().Skip(1).First();

        for (int i = 0; i < 3; i++)
            Debug.LogFormat("[Cookie Jars #{0}] One of the jars has {1} {2} inside.", _moduleId, cookieAmounts[i], cookieNames[cookies[i]].Replace("\n", " "));

        Debug.LogFormat("[Cookie Jars #{0}] The last eaten cookie was a{3} {1} cookie and the cookie eaten before that was a{4} {2} cookie.", _moduleId, cookieNamesLog[lastEaten], cookieNamesLog[lastLastEaten], cookieNamesLog[lastEaten] == "OREO" ? "n" : "", cookieNamesLog[lastLastEaten] == "OREO" ? "n" : "");
    }
}