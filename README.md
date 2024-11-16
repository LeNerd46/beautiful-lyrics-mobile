# Beautiful Lyrics Mobile

Tired of Spotify's boring lyrics? Hate Musixmatch's inaccuracy and basicness? This project aims to fix that by making lyrics *beautiful*, heavily based on [beautiful-lyrics](https://github.com/surfbryce/beautiful-lyrics) by [@surfbryce](https://github.com/surfbryce)

## Description

What exactly is beautiful lyrics? This is a **companian** app to Spotify, intended to be used along side it (for now). Its main feature is "beat by beat" synced lyrics or karaoke lyrics. In layman's terms, instead of Spotify's line by line, this is more like syllable by syllable. 

Along with displaying the lyrics already made out there, this app allows you to sync your own songs. Any line by line song that doesn't have *beautiful* lyrics, you can sync yourself so you don't have to go back to Spotify's *horrendous* lyrics.

## Getting Started

### Disclaimer

* This is ONLY for Android, there is no iOS version. Deal with it.
* In its current state, it is pretty bare bones. I am working on this a lot and it is always evolving. It originally was going to only be for displaying lyrics, but I decided to go even further. 

### Installing

I tried my best to build an APK, but it doesn't work at all when you install it. The only way to install it is to build and debug it yourself. It's pretty simple though, don't worry
It currently doesn't actually connect to Spotify, I'll have to fix that later. The code is all here, and it works, but you can't actually listen to songs right now, sorry

* Clone the repo
* You'll have to create your own Spotify Developer App in order to connect to Spotify. You can do that [here](https://developer.spotify.com/dashboard/applications), select the Android and Web API
* Replace my client ID and secret in `Platforms/Android/MainActivity.cs` and `AppShell.xaml.cs`
* This is the annoying part, you have to get your debug fingerprint, I explain how to do that [here](https://github.com/LeNerd46/SpotifyAppRemoteBinding)
* It should (not) work when you run it

## Help

* Ahh, it keeps crashing!
    * I am aware that it crashes a lot, this is because I've been focusing on adding new features instead of polishing it. This will get fixed later™
* How does syncing your own song work?
    * To sync your own song, you have to play a song that has line by line lyrics (there is currently no indicator on whether the song is line by line. If the lyrics don't load when you go to the lyrics page, it's probably line by line)
    * Tap the screen to start syncing the song. The song will go to the beginning and you can begin
    * When a word is sung, hold the screen for the duration of the word. DO NOT let go of the screen until the word is done being sung
    * Once you get to the last word, it will save the song and you can play back the song with *beautiful* lyrics. (This is word by word, without backing vocals. This will be improved later™)


## License

This project is licensed under the GNU GPLv3 License - see the LICENSE.md file for details