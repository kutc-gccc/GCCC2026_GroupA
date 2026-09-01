using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using GCCC.BoardGame.Presentation;
using GCCC.BoardGame.Presentation.Audio;
using GCCC.BoardGame.Presentation.Bootstrap;
using GCCC.BoardGame.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GCCC.BoardGame.Tests
{
    public sealed partial class BoardGameBootstrapTests
    {
        [UnityTest]
        public IEnumerator AudioControlsUpdateSingleManagerAndSources()
        {
            SceneManager.LoadScene(BoardGameSceneNames.Game, LoadSceneMode.Single);
            yield return null;

            BoardGameAudioManager[] managers =
                Object.FindObjectsByType<BoardGameAudioManager>(FindObjectsSortMode.None);
            Assert.That(managers, Has.Length.EqualTo(1));

            Slider bgmSlider = GameObject.Find("BGM Slider").GetComponent<Slider>();
            Slider sfxSlider = GameObject.Find("SFX Slider").GetComponent<Slider>();
            bgmSlider.value = 0.4f;
            sfxSlider.value = 0.35f;
            yield return null;

            Assert.That(managers[0].BgmVolume, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(managers[0].SfxVolume, Is.EqualTo(0.35f).Within(0.001f));

            AudioSource bgmSource = GameObject.Find("BGM Source").GetComponent<AudioSource>();
            AudioSource sfxSource = GameObject.Find("SFX Source").GetComponent<AudioSource>();
            Assert.That(bgmSource.loop, Is.True);
            Assert.That(bgmSource.volume, Is.EqualTo(0.04f).Within(0.001f));
            Assert.That(sfxSource.loop, Is.False);
            Assert.That(sfxSource.volume, Is.EqualTo(0.35f).Within(0.001f));

            bootstrapObject = Object.FindFirstObjectByType<BoardGameBootstrap>().gameObject;
            bootstrap = bootstrapObject.GetComponent<BoardGameBootstrap>();
        }

    }
}
