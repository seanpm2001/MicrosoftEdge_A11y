﻿using Interop.UIAutomationCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Edge.A11y
{
    /// <summary>
    /// This is where the logic of the tests is stored
    /// </summary>
    class TestData
    {
        public const string ARFAIL = "Failed additional requirement";
        public const string ARPASS = "";
        /// <summary>
        /// The name of the test, which corresponds to the name of the html element
        /// </summary>
        public string _TestName;
        /// <summary>
        /// The name of the UIA control type we will use to search for the element
        /// </summary>
        public string _ControlType;
        /// <summary>
        /// The name of the UIA localized control type, which will be part of the test
        /// case if it is not null
        /// </summary>
        public string _LocalizedControlType;
        /// <summary>
        /// The name of the UIA landmark type, which will be part of the test
        /// case if it is not null
        /// </summary>
        public string _LandmarkType;
        /// <summary>
        /// The name of the UIA localized landmark type, which will be part of the test
        /// case if it is not null
        /// </summary>
        public string _LocalizedLandmarkType;
        /// <summary>
        /// A list of ids for all the elements that should be keyboard accessible (via tab)
        /// </summary>
        public List<string> _KeyboardElements;
        /// <summary>
        /// A func that allows extending the tests for specific elements. If an empty string is
        /// returned, the element passes. Otherwise, an explanation of its failure is returned.
        /// </summary>
        public Func<List<IUIAutomationElement>, DriverManager, List<string>, string> _AdditionalRequirement;
        /// <summary>
        /// If not null, this func will be used to test elements to see if they should be
        /// tested (instead of matching _ControlType).
        /// </summary>
        public Func<IUIAutomationElement, bool> _SearchStrategy;

        /// <summary>
        /// Simple ctor
        /// </summary>
        /// <param name="testName"></param>
        /// <param name="controlType"></param>
        /// <param name="localizedControlType"></param>
        /// <param name="landmarkType"></param>
        /// <param name="localizedLandmarkType"></param>
        /// <param name="keyboardElements"></param>
        /// <param name="additionalRequirement"></param>
        public TestData(string testName,
            string controlType,
            string localizedControlType = null,
            string landmarkType = null,
            string localizedLandmarkType = null,
            List<string> keyboardElements = null,
            Func<List<IUIAutomationElement>, DriverManager, List<string>, string> additionalRequirement = null,
            Func<IUIAutomationElement, bool> searchStrategy = null)
        {
            _TestName = testName;
            _ControlType = controlType;
            _LocalizedControlType = localizedControlType;
            _LandmarkType = landmarkType;
            _LocalizedLandmarkType = localizedLandmarkType;
            _KeyboardElements = keyboardElements;
            _AdditionalRequirement = additionalRequirement;
            _SearchStrategy = searchStrategy;
        }

        //All the tests to run
        public static Lazy<List<TestData>> alltests = new Lazy<List<TestData>>(AllTests);

        /// <summary>
        /// Get the TestData for the given test page
        /// </summary>
        /// <param name="testName">The name of the file being tested</param>
        /// <returns>TestData for the given test page, or null if it couldn't be found</returns>
        public static TestData DataFromName(string testName)
        {
            return alltests.Value.FirstOrDefault(t => t._TestName == testName);
        }

        /// <summary>
        /// Singleton initializer
        /// </summary>
        /// <returns></returns>
        static List<TestData> AllTests()
        {
            var converter = new ElementConverter();
            const int timeout = 0;
            var alltests = new List<TestData>{
                new TestData("article", "Group", "article",
                    additionalRequirement: CheckElementNames(7,
                    new List<string>{
                        "aria-label attribute 3",
                        "h1 referenced by aria-labelledby4",
                        "title attribute 5",
                        "aria-label attribute 7"},
                    new List<string>{
                        "h1 referenced by aria-describedby6",
                        "title attribute 7"
                    })),
                new TestData("aside", "Group", "aside", "Custom", "complementary",
                    additionalRequirement: CheckElementNames(7,
                    new List<string>{
                        "aria-label attribute 3",
                        "h1 referenced by aria-labelledby4",
                        "title attribute 5",
                        "aria-label attribute 7"},
                    new List<string>{
                        "h1 referenced by aria-describedby6",
                        "title attribute 7"
                    })),
                new TestData("audio", "Group", "audio",
                    additionalRequirement: ((elements, driver, ids) => {
                        var childNames = CheckChildNames(new List<string> {
                                "Play",
                                "Time elapsed",
                                "Seek",
                                "Time remaining",
                                "Mute",
                                "Volume"})(elements, driver, ids);//TODO ensure nothing else is read
                        if(childNames != ARPASS){
                            return childNames;
                        }
                        return CheckAudioKeyboardInteractions(elements, driver, ids);
                    })),
                new TestData("canvas", "Image",
                    additionalRequirement: ((elements, driver, ids) => {
                        var browserElement = EdgeA11yTools.FindBrowserDocument(0);
                        var automationElementConverter = new ElementConverter();

                        HashSet<string> foundControlTypes;
                        elements = EdgeA11yTools.SearchDocumentChildren(browserElement, "", (current) => {
                            var convertedRole = automationElementConverter.GetElementNameFromCode(current.CurrentControlType);
                            return convertedRole == "Button" || convertedRole == "Text";
                        }, out foundControlTypes);

                        return elements.Count() == 2 ? ARPASS : "Unable to find subdom elements";
                    })),//TODO feature detection focus ring
                new TestData("datalist", "Combobox", keyboardElements: new List<string> { "input1" },
                    additionalRequirement: ((elements, driver, ids) => {
                        if(elements.Any(e => e.CurrentControllerFor == null || e.CurrentControllerFor.Length == 0)){
                            return "Element controller for not set";
                        }

                        Func<string, string> datalistValue = (id) => (string)driver.ExecuteScript("return document.getElementById('" + id + "').value", 0);
                        foreach (var id in ids)
                        {
                            var initial = datalistValue(id);
                            driver.SendSpecialKeys(id, "Arrow_downEnter");
                            if (datalistValue(id) == initial)
                            {
                                return "Unable to set the datalist with keyboard";
                            }
                        }

                        return ARPASS;
                    })),
                new TestData("details", null),
                new TestData("dialog", null),
                new TestData("figure", "Group", "figure",
                    additionalRequirement: CheckElementNames(7,
                    new List<string>{
                        "aria-label attribute 2",
                        "p referenced by aria-labelledby3",
                        "title attribute 4",
                        "Figcaption element 5",
                        "Figcaption element 7"},
                    new List<string>{
                        "p referenced by aria-describedby6",
                        "title attribute 7"
                    })),
                //new TestData("figure-figcaption", "Text",
                    //additionalRequirement: ((elements, driver, ids) =>
                        //elements.All(element => element.CurrentName == "HTML5 logo") ?
                        //ARPASS : ARFAIL)),//TODO TextPattern range says HTML5 logo
                new TestData("footer", "Group",
                    additionalRequirement: (elements, driver, ids) => {
                        var result = CheckElementNames(7,
                            new List<string>{
                                "aria-label attribute 3",
                                "small referenced by aria-labelledby4",
                                "title attribute 5",
                                "aria-label attribute 7"},
                            new List<string>{
                                "small referenced by aria-describedby6",
                                "title attribute 7"
                            })(elements, driver, ids);

                        if (result != ARPASS)
                        {
                            return result;
                        }

                        //not all elements need to have the same localized control type
                        if(elements.Select(e => e.CurrentLocalizedControlType).Count(lct => lct == "footer") != 6){
                            return "Elements did not have the correct localized control types";
                        }

                        var elementConverter = new ElementConverter();

                        var convertedLandmarks = 0;
                        var localizedLandmarks = 0;
                        //same for landmark and localizedlandmark
                        foreach (var element in elements)
                        {
                            var five = element as IUIAutomationElement5;
                            var convertedLandmark = elementConverter.GetElementNameFromCode(five.CurrentLandmarkType);
                            var localizedLandmark = five.CurrentLocalizedLandmarkType;
                            if (convertedLandmark == "Custom")
                            {
                                convertedLandmarks++;
                            }
                            if (localizedLandmark == "content information")
                            {
                                localizedLandmarks++;
                            }
                        }
                        if (convertedLandmarks != 6)
                        {
                            return "Elements did not have the correct landmark types";
                        }

                        if (localizedLandmarks != 6)
                        {
                            return "Elements did not have the correct localized landmark types";
                        }

                        return ARPASS;
                    }),
                new TestData("header", "Group",
                    additionalRequirement: (elements, driver, ids) => {
                        var result = CheckElementNames(7,
                            new List<string>{
                                "aria-label attribute 3",
                                "small referenced by aria-labelledby4",
                                "title attribute 5",
                                "aria-label attribute 7"},
                            new List<string>{
                                "small referenced by aria-describedby6",
                                "title attribute 7"
                            })(elements, driver, ids);

                        if (result != ARPASS)
                        {
                            return result;
                        }

                        //not all elements need to have the same localized control type
                        if(elements.Select(e => e.CurrentLocalizedControlType).Count(lct => lct == "header") != 6){
                            return "Elements did not have the correct localized control types";
                        }

                        var elementConverter = new ElementConverter();

                        var convertedLandmarks = 0;
                        var localizedLandmarks = 0;
                        //same for landmark and localizedlandmark
                        foreach (var element in elements)
                        {
                            var five = element as IUIAutomationElement5;
                            var convertedLandmark = elementConverter.GetElementNameFromCode(five.CurrentLandmarkType);
                            var localizedLandmark = five.CurrentLocalizedLandmarkType;
                            if (convertedLandmark == "Custom")
                            {
                                convertedLandmarks++;
                            }
                            if (localizedLandmark == "banner")
                            {
                                localizedLandmarks++;
                            }
                        }
                        if (convertedLandmarks != 6)
                        {
                            return "Elements did not have the correct landmark types";
                        }

                        if (localizedLandmarks != 6)
                        {
                            return "Elements did not have the correct localized landmark types";
                        }

                        return ARPASS;
                    }),
                new TestData("input-color", "Button", "color picker",
                        //TODO naming
                    additionalRequirement: (elements, driver, ids) =>
                        ids.FirstOrDefault(id =>
                        {
                            Func<string> CheckColorValue = () => (string) driver.ExecuteScript("return document.getElementById('"+ id + "').value", timeout);
                            var initial = CheckColorValue();

                            driver.SendSpecialKeys(id, "EnterTabEscape");
                            if (CheckColorValue() != initial)
                            {
                                return true;
                            }

                            //TODO open with space as well
                            driver.SendSpecialKeys(id, "EnterTabEnter");
                            if (CheckColorValue() == initial)
                            {
                                return true;
                            }

                            initial = CheckColorValue();

                            driver.SendSpecialKeys(id, "EnterTabTabArrow_rightArrow_rightArrow_right");
                            if (CheckColorValue() == initial)
                            {
                                return true;
                            }

                            //p1
                            //TODO get to buttons
                            //TODO active buttons with space/enter

                            //p2
                            //TODO enter submits escape cancels
                            //TODO move sliders with left right
                            //TODO children have correct sliders(CT: Slider,
                            //Range.RangeValue) is whatever will be submitted
                            //TODO root button has controllerfor pointing to dialog
                            //TODO ControllerFor and LiveSetting=polite for color well

                            return false;
                        }) == null ? ARPASS : "Failed keyboard interaction"
                    ),
                new TestData("input-date", "Edit", keyboardElements: new List<string> { "input1", "input2" },
                        //TODO naming
                    additionalRequirement: CheckCalendarKeyboard(3)),
                new TestData("input-datetime-local", "Text", additionalRequirement: CheckDatetimeLocalKeyboard()),
                    //TODO naming
                new TestData("input-email", "Edit", "email",
                    //TODO naming
                    keyboardElements: new List<string> { "input1", "input2" },
                    additionalRequirement: CheckValidation()),
                new TestData("input-month", "Edit", keyboardElements: new List<string> { "input1", "input2" },
                    //TODO naming
                    additionalRequirement: CheckCalendarKeyboard(2)),
                new TestData("input-number", "Spinner", "number",
                    keyboardElements: new List<string> { "input1", "input2" },
                    additionalRequirement: CheckValidation()),
                    //TODO naming
                new TestData("input-range", "Slider", keyboardElements: new List<string> { "input1", "input2" },
                    //TODO naming
                    additionalRequirement: (elements, driver, ids) => {
                        foreach(var id in ids){
                            Func<int> RangeValue = () => (int) Int32.Parse((string) driver.ExecuteScript("return document.getElementById('" + id + "').value", 0));

                            var initial = RangeValue();
                            driver.SendSpecialKeys(id, "Arrow_up");
                            if (initial == RangeValue())
                            {
                                return "Unable to increase range with arrow up";
                            }
                            driver.SendSpecialKeys(id, "Arrow_down");
                            if (initial != RangeValue())
                            {
                                return "Unable to decrease range with arrow down";
                            }

                            driver.SendSpecialKeys(id, "Arrow_right");
                            if (initial >= RangeValue())
                            {
                                return "Unable to increase range with arrow right";
                            }
                            driver.SendSpecialKeys(id, "Arrow_left");
                            if (initial != RangeValue())
                            {
                                return "Unable to decrease range with arrow left";
                            }
                        }
                        foreach(var element in elements){
                            if (!element.GetPatterns().Contains("RangeValuePattern")) { 
                                return "Element did not implement the RangeValuePattern";
                            }
                        }
                        return CheckElementNames(7,
                            new List<string>
                            {
                                "aria-label attribute 2",
                                "p referenced by aria-labelledby3",
                                "label wrapping input 4",
                                "title attribute 5",
                                "label referenced by for/id attributes 7",
                            },
                            new List<string>
                            {
                                "p referenced by aria-describedby6",
                                "title attribute 7"
                            })(elements, driver, ids);
                    }),
                new TestData("input-search", "Edit", "search", keyboardElements: new List<string> { "input1", "input2" }),
                    //TODO naming
                new TestData("input-tel", "Edit", "telephone", keyboardElements: new List<string> { "input1", "input2" }),
                    //TODO naming
                new TestData("input-time", "Edit", keyboardElements: new List<string> { "input1", "input2" },
                    //TODO naming
                    additionalRequirement: CheckCalendarKeyboard(2)),
                new TestData("input-url", "Edit", "url",
                        keyboardElements: new List<string> { "input1", "input2" },
                        additionalRequirement: CheckValidation()),
                new TestData("input-week", "Edit", keyboardElements: new List<string> { "input1", "input2" },
                    additionalRequirement: CheckCalendarKeyboard(2)),
                new TestData("main", "Group", "main", "Main", "main",
                    additionalRequirement: CheckElementNames(6,
                    new List<string>{
                        "title attribute 1",
                        "aria-label attribute 2",
                        "h1 referenced by aria-labelledby3",
                        "title attribute 4",
                        "aria-label attribute 6"
                    },
                    new List<string>{
                        "h1 referenced by aria-describedby5",
                        "title attribute 6"
                    })),
                new TestData("mark", "Text", "mark"),
                new TestData("meter", "Progressbar", "meter",
                        //TODO naming
                    additionalRequirement:
                        ((elements, driver, ids) => {
                            if(!elements.All(element => element.GetProperties().Any(p => p.Contains("IsReadOnly")))){
                                return "Not all elements were read only";
                            }
                            return CheckElementNames(7,
                                new List<string>
                                {
                                    "aria-label attribute 2",
                                    "p referenced by aria-labelledby3",
                                    "label wrapping meter 4",
                                    "title attribute 5",
                                    "label referenced by for/id attributes 7",
                                },
                                new List<string>
                                {
                                    "p referenced by aria-describedby6",
                                    "title attribute 7"
                                })(elements, driver, ids);
                        }),
                    searchStrategy: (element => element.GetPatterns().Contains("RangeValuePattern"))),//NB the ControlType is not used for searching this element
                new TestData("menuitem", null),
                new TestData("menupopup", null),
                new TestData("menutoolbar", null),
                new TestData("nav", "Group", "navigation", "Navigation", "navigation",
                    additionalRequirement: CheckElementNames(6,
                    new List<string>{
                        "aria-label attribute 2",
                        "h1 referenced by aria-labelledby3",
                        "title attribute 4",
                        "aria-label attribute 6"},
                    new List<string>{
                        "h1 referenced by aria-describedby5",
                        "title attribute 6"
                    })),
                new TestData("output", "Group", "output",
                    additionalRequirement: ((elements, driver, ids) => {
                        if (!elements.All(element => ((IUIAutomationElement5)element).CurrentLiveSetting != LiveSetting.Polite)){
                            return "Element did not have LiveSetting = Polite";
                        }
                        if (!elements.All(element => element.CurrentControllerFor != null && element.CurrentControllerFor.Length > 0)){
                            //TODO points to output from input
                            return "Element did not have ControllerFor set";
                        }
                        return ARPASS;
                    })),
                new TestData("progress", "Progressbar",
                    additionalRequirement: CheckElementNames(7,
                    new List<string>{
                        "aria-label attribute 2",
                        "p referenced by aria-labelledby3",
                        "label wrapping output 4",
                        "title attribute 5",
                        "label referenced by for/id attributes 7"
                    },
                    new List<string>{
                        "p referenced by aria-describedby6",
                        "title attribute 7"
                    })),
                new TestData("section", "Group", "section", "Custom", "region",
                        //TODO if there's no accessible name, do not include in the tree
                    additionalRequirement: CheckElementNames(7,
                    new List<string>{
                        "aria-label attribute 3",
                        "h1 referenced by aria-labelledby4",
                        "title attribute 5",
                        "aria-label attribute 7"},
                    new List<string>{
                        "h1 referenced by aria-describedby6",
                        "title attribute 7"
                    })),
                new TestData("summary", null),
                new TestData("time", "Group", "time",
                    additionalRequirement: ((elements, driver, ids) => {
                        if (!elements.All(element => {
                            var fullDescription = ((IUIAutomationElement6)element).CurrentFullDescription;
                            return fullDescription != null && fullDescription == "2015-10-28";}))
                        {
                            return "Element did not have the correct FullDescription";
                        }
                        return ARPASS;
                    })),
                new TestData("track", "track",
                    additionalRequirement: ((elements, driver, ids) =>
                    {
                        driver.ExecuteScript(Javascript.Track, timeout);

                        return (bool)driver.ExecuteScript("return Modernizr.track && Modernizr.texttrackapi", timeout) ? ARPASS :
                            "Element was not found to be supported by Modernizr";
                    }),
                    searchStrategy: (element => true)),
                new TestData("video", "Group", null, keyboardElements: new List<string> { "video1" },
                    additionalRequirement: ((elements, driver, ids) =>
                        CheckChildNames(
                            new List<string> {
                                    "Play",
                                    "Time elapsed",
                                    "Seek",
                                    "Time remaining",
                                    "Zoom in",
                                    "Show audio",
                                    "Show captioning",
                                    "Mute",
                                    "Volume",
                                    "Full screen" })(elements, driver, ids) == ARPASS ?
                        CheckVideoKeyboardInteractions(elements, driver, ids) : ARFAIL)),
                new TestData("hidden-att", "Button", null,
                        //TODO no text pattern
                    additionalRequirement: ((elements, driver, ids) =>
                    {
                        var elementConverter = new ElementConverter();
                        var paneCode = elementConverter.GetElementCodeFromName("Pane");

                        var browserElement = EdgeA11yTools.FindBrowserDocument(0);

                        if (elements.Count(e => e.CurrentControlType != paneCode) != 0)
                        {
                            return "Found " + elements.Count(e => e.CurrentControlType != paneCode) + " elements. Expected 0";
                        }

                        driver.ExecuteScript(Javascript.RemoveHidden, timeout);

                        HashSet<string> foundControlTypes;
                        elements = EdgeA11yTools.SearchDocumentChildren(browserElement, "Button", null, out foundControlTypes);
                        if (elements.Count(e => e.CurrentControlType != paneCode) != 1)
                        {
                            return "Found " + elements.Count(e => e.CurrentControlType != paneCode) + " elements. Expected 1";
                        }

                        //TODO possibly remove
                        driver.ExecuteScript(Javascript.RemoveAriaHidden, timeout);

                        elements = EdgeA11yTools.SearchDocumentChildren(browserElement, "Button", null, out foundControlTypes);
                        if (elements.Count(e => e.CurrentControlType != paneCode) != 2)
                        {
                            return "Found " + elements.Count(e => e.CurrentControlType != paneCode) + " elements. Expected 2";
                        }

                        return ARPASS;
                    }),
                    searchStrategy: (element => true)),//take all elements
                new TestData("required-att", "Edit",
                    additionalRequirement: (elements, driver, ids) =>
                    {
                        driver.SendSubmit("input1");
                        System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(500));
                        foreach(var element in elements){
                            if(element.CurrentControllerFor == null || element.CurrentControllerFor.Length == 0){
                                return "Element did not have controller for set";
                            }

                            if(element.CurrentIsDataValidForForm != 0){
                                return "Element did not have IsDataValidForForm set to false";
                            }

                            if(element.CurrentIsRequiredForForm != 1){
                                return "Element did not have IsRequiredForForm set to true";
                            }

                            if(element.CurrentHelpText == null || element.CurrentHelpText.Length == 0){
                                return "Element did not have HelpText";
                            }
                        }

                        return ARPASS;
                    }),
                new TestData("placeholder-att", "Edit",
                    additionalRequirement: ((elements, driver, ids) =>
                    {
                        if (elements.Count() != 6)
                        {
                            return ARFAIL;
                        }

                        var elementNames = elements.Select(element => element.CurrentName).ToList();
                        return new List<Func<List<string>, bool>>{
                            names => names.Contains("placeholder text 1"),
                            names => names.Contains("Label text 2:"),
                            names => names.Contains("Label text 3:"),
                            names => names.Contains("placeholder text 4"),
                            names => names.Contains("placeholder text 5"),
                            names => names.Contains("aria-placeholder text 6"),
                        }.All(f => f(elementNames)) ? ARPASS : ARFAIL;
                    }))
            };

            return alltests;
        }

        /// <summary>
        /// Check basic keyboard interactions for the video control
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="driver"></param>
        /// <param name="ids"></param>
        /// <returns>An empty string if an element fails, otherwise an explanation</returns>
        private static string CheckVideoKeyboardInteractions(List<IUIAutomationElement> elements, DriverManager driver, List<string> ids)
        {
            string videoId = "video1";
            Func<bool> VideoPlaying = () => (bool)driver.ExecuteScript("return !document.getElementById('" + videoId + "').paused", 0);
            Func<double> VideoVolume = () => driver.ExecuteScript("return document.getElementById('" + videoId + "').volume", 0).ParseMystery();
            Func<bool> VideoMuted = () => (bool)driver.ExecuteScript("return document.getElementById('" + videoId + "').muted", 0);
            Func<double> VideoElapsed = () => driver.ExecuteScript("return document.getElementById('" + videoId + "').currentTime", 0).ParseMystery();

            //Case 1: tab to video element and play/pause
            driver.SendSpecialKeys(videoId, "Space");
            if (!VideoPlaying())
            {
                return "Video was not playing after spacebar on root element";
            }
            driver.SendSpecialKeys(videoId, "Space");
            if (VideoPlaying())
            {
                return "Video was not paused after spacebar on root element";
            }

            //Case 2: tab to play button and play/pause
            driver.SendSpecialKeys(videoId, "TabSpace");
            if (!VideoPlaying())
            {
                return "Video was not playing after spacebar on play button";
            }
            driver.SendSpecialKeys(videoId, "Enter");
            if (VideoPlaying())
            {
                return "Video was not paused after enter on play button";
            }

            //Case 3: Volume and mute
            Javascript.ClearFocus(driver, 0);
            driver.SendTabs(videoId, 6);//tab to volume control //TODO make this more resilient to UI changes
            var initial = VideoVolume();
            driver.SendSpecialKeys(videoId, "Arrow_downArrow_down");//volume down
            if (initial == VideoVolume())
            {
                return "Volume did not decrease with arrow keys";
            }
            driver.SendSpecialKeys(videoId, "Arrow_upArrow_up");//volume up
            if (VideoVolume() != initial)
            {
                return "Volume did not increase with arrow keys";
            }
            driver.SendSpecialKeys(videoId, "Enter");//mute//TODO switch back to original order once space works
            if (!VideoMuted())
            {
                return "Enter did not mute the video";
            }
            driver.SendSpecialKeys(videoId, "Space");//unmute
            if (VideoMuted())
            {
                return "Space did not unmute the video";
            }

            //Case 4: Audio selection
            Javascript.ClearFocus(driver, 0);
            driver.SendTabs(videoId, 5);//tab to audio selection//TODO make this more resilient to UI changes
            driver.SendSpecialKeys(videoId, "EnterArrow_down");

            //Case 5: Progress and seek
            if (VideoPlaying())
            { //this should not be playing
                return "Video was playing when it shouldn't have been";
            }
            Javascript.ClearFocus(driver, 0);
            driver.SendTabs(videoId, 3);//tab to seek//TODO make this more resilient to UI changes
            initial = VideoElapsed();
            driver.SendSpecialKeys(videoId, "Arrow_right"); //skip ahead
            if (initial != VideoElapsed() - 10)
            {
                return "Video did not skip forward with arrow right";
            }

            driver.SendSpecialKeys(videoId, "Arrow_left"); //skip back
            if (initial != VideoElapsed())
            {
                return "Video did not skip back with arrow left";
            }

            //Case 6: Progress and seek on remaining time
            if (VideoPlaying())
            { //this should not be playing
                return "Video was playing when it shouldn't have been";
            }
            Javascript.ClearFocus(driver, 0);
            driver.SendTabs(videoId, 4);//tab to seek//TODO make this more resilient to UI changes
            initial = VideoElapsed();
            driver.SendSpecialKeys(videoId, "Arrow_right"); //skip ahead
            if (initial != VideoElapsed() - 10)
            {
                return "Video did not skip forward with arrow right";
            }

            driver.SendSpecialKeys(videoId, "Arrow_left"); //skip back
            if (initial != VideoElapsed())
            {
                return "Video did not skip back with arrow left";
            }

            //TODO check fullscreen

            return ARPASS;
        }

        /// <summary>
        /// Check basic keyboard interactions for the audio control
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="driver"></param>
        /// <param name="ids"></param>
        /// <returns>An empty string if an element fails, otherwise an explanation</returns>
        private static string CheckAudioKeyboardInteractions(List<IUIAutomationElement> elements, DriverManager driver, List<string> ids)
        {
            string audioId = "audio1";
            Func<bool> AudioPlaying = () => (bool)driver.ExecuteScript("return !document.getElementById('" + audioId + "').paused", 0);
            Func<double> AudioVolume = () => driver.ExecuteScript("return document.getElementById('" + audioId + "').volume", 0).ParseMystery();
            Func<bool> AudioMuted = () => (bool)driver.ExecuteScript("return document.getElementById('" + audioId + "').muted", 0);
            Func<double> AudioElapsed = () => driver.ExecuteScript("return document.getElementById('" + audioId + "').currentTime", 0).ParseMystery();

            //Case 1: Play/Pause
            driver.SendTabs(audioId, 1); //Tab to play button
            driver.SendSpecialKeys(audioId, "Enter");
            if (!AudioPlaying())
            {
                return "Audio did not play with enter";
            }

            driver.SendSpecialKeys(audioId, "Space");
            if (AudioPlaying())
            {
                return "Audio did not pause with space";
            }

            //Case 2: Seek
            if (AudioPlaying())
            {
                return "Audio was playing when it shouldn't have been";
            }
            driver.SendTabs(audioId, 3);
            var initial = AudioElapsed();
            driver.SendSpecialKeys(audioId, "Arrow_right");
            if (initial == AudioElapsed())
            {
                return "Audio did not skip forward with arrow right";
            }
            driver.SendSpecialKeys(audioId, "Arrow_left");
            if (initial != AudioElapsed())
            {
                return "Audio did not skip back with arrow left";
            }

            //Case 3: Volume and mute
            Javascript.ClearFocus(driver, 0);
            driver.SendTabs(audioId, 5);
            initial = AudioVolume();
            driver.SendSpecialKeys(audioId, "Arrow_down");
            if (initial == AudioVolume())
            {
                return "Volume did not decrease with arrow down";
            }

            driver.SendSpecialKeys(audioId, "Arrow_up");
            if (initial != AudioVolume())
            {
                return "Volume did not increase with arrow up";
            }

            driver.SendSpecialKeys(audioId, "Space");
            if (!AudioMuted())
            {
                return "Audio was not muted by space on the volume control";
            }

            driver.SendSpecialKeys(audioId, "Enter");
            if (AudioMuted())
            {
                return "Audio was not unmuted by enter on the volume control";
            }

            return ARPASS;
        }

        /// <summary>
        /// Test all date/time elements except for datetime-local, which is tested by the
        /// amended method below.
        /// </summary>
        /// <param name="fields">A count of the number of fields to test</param>
        /// <returns></returns>
        public static Func<List<IUIAutomationElement>, DriverManager, List<string>, string> CheckCalendarKeyboard(int fields)
        {
            //TODO child elements in UIA tree nested lists and two buttons
            //TODO tab to buttons
            return new Func<List<IUIAutomationElement>, DriverManager, List<string>, string>((elements, driver, ids) =>
            {
                var result = ids.FirstOrDefault(id =>//"first element that fails"
                {
                    driver.SendSpecialKeys(id, "EnterEscapeEnterEnter");//Make sure that the element has focus (gets around weirdness in WebDriver)

                    Func<string> DateValue = () => (string)driver.ExecuteScript("return document.getElementById('" + id + "').value", 0);

                    var today = DateValue();

                    //Open the menu
                    driver.SendSpecialKeys(id, "Enter");
                    //Change each field in the calendar
                    for (int i = 0; i < fields; i++)
                    {
                        driver.SendSpecialKeys(id, "Arrow_downTab");
                    }
                    //Close the menu (only necessary for time)
                    driver.SendSpecialKeys(id, "Enter");

                    //Get the altered value, which should be one off the default
                    //for each field
                    var newdate = DateValue();
                    var newdatesplit = newdate.Split('-', ':');
                    var todaysplit = today.Split('-', ':');

                    //ensure that all fields have been changed
                    for (int i = 0; i < fields; i++)
                    {
                        if (newdatesplit[i] == todaysplit[i])
                        {
                            return true;//true means this element fails
                        }
                    }

                    return false;
                });
                if (result == null)
                {
                    return ARPASS;
                }
                return "Keyboard interaction failed for element with id: " + result;
            });
        }

        /// <summary>
        /// Test the datetime-local input element with an amended version of the method
        /// above.
        /// </summary>
        /// <returns></returns>
        public static Func<List<IUIAutomationElement>, DriverManager, List<string>, string> CheckDatetimeLocalKeyboard()
        {
            return new Func<List<IUIAutomationElement>, DriverManager, List<string>, string>((elements, driver, ids) =>
            {
                var result = ids.FirstOrDefault(id =>//"first element that fails"
                {
                    driver.SendSpecialKeys(id, "EnterEscapeEnterEnter");//Make sure that the element has focus (gets around weirdness in WebDriver)

                    var inputFields = new List<int> { 3, 3 };
                    var outputFields = 5;
                    Func<string> DateValue = () => (string)driver.ExecuteScript("return document.getElementById('" + id + "').value", 0);

                    driver.SendSpecialKeys(id, "EnterEnterTabEnterEnter");

                    var today = DateValue();

                    driver.SendSpecialKeys(id, "ShiftTabShift");//Shift focus back to main control
                    foreach (var count in inputFields)
                    {
                        //Open the menu
                        driver.SendSpecialKeys(id, "Enter");
                        //Change each field in the calendar
                        for (int i = 0; i < count; i++)
                        {
                            driver.SendSpecialKeys(id, "Arrow_downTab");
                        }
                    }

                    //Get the altered value, which should be one off the default
                    //for each field
                    var newdate = DateValue();
                    var newdatesplit = newdate.Split('-', 'T', ':');
                    var todaysplit = today.Split('-', 'T', ':');

                    //ensure that all fields have been changed
                    for (int i = 0; i < outputFields; i++)
                    {
                        if (newdatesplit[i] == todaysplit[i])
                        {
                            return true;
                        }
                    }

                    return false;
                });
                if (result == null)
                {
                    return ARPASS;
                }
                return "Keyboard interaction failed for element with id: " + result;
            });
        }

        /// <summary>
        /// Func factory for checking that when invalid input is entered into a form,
        /// an error message appears.
        /// </summary>
        /// <returns></returns>
        public static Func<List<IUIAutomationElement>, DriverManager, List<string>, string> CheckValidation()
        {
            return (elements, driver, ids) =>
                {
                    //The indices of the elements that have been found to be invalid before
                    var previouslyInvalid = new HashSet<int>();
                    for (var i = 0; i < elements.Count; i++)
                    {
                        driver.SendKeys("input" + (i + 1), "invalid");
                        driver.SendSubmit("input" + (i + 1));
                        System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(500));

                        //Everything that is invalid on the page
                        var invalid = elements.Where(e => e.CurrentControllerFor != null &&
                                        e.CurrentControllerFor.Length > 0 &&
                                        e.CurrentIsDataValidForForm == 0 && 
                                        e.CurrentHelpText != null &&
                                        e.CurrentHelpText.Length > 0).Select(e => elements.IndexOf(e));

                        //Elements that are invalid for the first time
                        var newInvalid = invalid.DefaultIfEmpty(-1).FirstOrDefault(inv => !previouslyInvalid.Contains(inv));
                        if (newInvalid == -1)
                        {
                            return "Element failed to validate improper input";
                        }

                        if (elements[newInvalid].CurrentControllerFor.Length > 1)
                        {
                            throw new Exception("Test assumption failed: expected only one controller");
                        }
                        var helpPane = elements[newInvalid].CurrentControllerFor.GetElement(0);
                        if (helpPane.CurrentControlType != new ElementConverter().GetElementCodeFromName("Pane"))
                        {
                            return "Error message did not have correct ControlType";
                        }
                        //TODO find out if the message pane needs to have any more requirements

                        previouslyInvalid.Add(newInvalid);
                    }
                    return ARPASS;
                };
        }

        /// <summary>
        /// Func factory for checking that the required child elements are in the accessibility tree.
        /// </summary>
        /// <param name="requiredNames">The names of the elements to search for</param>
        /// <returns>A Func that can be used to verify whether the elements in the list are child elements</returns>
        public static Func<List<IUIAutomationElement>, DriverManager, List<string>, string> CheckChildNames(List<string> requiredNames)
        {
            return (elements, driver, ids) =>
            {
                foreach (var element in elements)
                {
                    var names = element.GetChildNames();
                    var firstFail = requiredNames.FirstOrDefault(rn => !names.Any(n => n.Contains(rn)));
                    if (firstFail != null)
                    {
                        return "Failed to find " + firstFail;
                    }
                }
                return ARPASS;
            };
        }

        /// <summary>
        /// Func factory for checking that elements have the proper Names and FullDescriptions
        /// </summary>
        /// <param name="total">How many elements are on the page total, since
        /// some are supposed to be blank</param>
        /// <param name="requiredNames">All the names we expect to find</param>
        /// <param name="requiredDescriptions">All the descriptions we expect
        /// to find</param>
        /// <returns>A func that can be used to check the names and descriptions
        /// of elements</returns>
        public static Func<List<IUIAutomationElement>, DriverManager, List<string>, string> CheckElementNames(int total, List<string> requiredNames, List<string> requiredDescriptions)
        {
            return (elements, driver, ids) =>
            {
                var names = elements.ConvertAll(element => element.CurrentName);
                var descriptions = elements.ConvertAll(element => ((IUIAutomationElement6)element).CurrentFullDescription);

                //Check names
                if (total != names.Count(name => name == "") + requiredNames.Count())
                {
                    return total - requiredNames.Count() + " names should have been blank. Found " + names.Count(name => name == "");
                }
                foreach (var requiredName in requiredNames)
                {
                    if (!names.Contains(requiredName))
                    {
                        return GuessElementNumber(requiredName) + " had incorrect name";
                    }
                }

                //Check descriptions
                if (total != descriptions.Count(description => description == "") + requiredDescriptions.Count())
                {
                    return total - requiredDescriptions.Count() + " descriptions should have been blank. Found " + descriptions.Count(description => description == "");
                }
                foreach (var requiredDescription in requiredDescriptions)
                {
                    if (!descriptions.Contains(requiredDescription))
                    {
                        return GuessElementNumber(requiredDescription) + " had incorrect description";
                    }
                }

                return ARPASS;
            };
        }

        /// <summary>
        /// From the name of the string we were supposed to find,
        /// guess what element failed
        /// </summary>
        /// <param name="property">The string we were supposed to find</param>
        /// <returns></returns>
        private static string GuessElementNumber(string property)
        {
            var digit = property.FirstOrDefault(c => Char.IsNumber(c));
            return digit == null ? "An element" : digit.ToString();
        }
    }
}
