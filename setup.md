
# Beautiful Lyrics Mobile Setup

This guide will show you how to create a Spotify Developer App and get the necessary information in order for the app to function properly. 


## Why Do You Need to Create a Developer App?
It would be really awesome if I could just have everyone use the app without needing to do this, but Spotify unfortunately is very slow with accepting new applications. Hopefully this will not be necessary in the future, but I would rather release the app now and do this than wait 6 months for them to review it

# Creating the App

- Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
- Create an app
- For the redirect URL's, add https://beautifullyrics.lenerd.tech/api/spotify/success
- Spotify requires the Android app package name. The package name is `com.lenerd46.beautifullyrics` and the SHA1 fingerprint is `FD:9C:96:42:D8:F5:D7:EB:F0:8A:CE:F1:47:3D:D5:F9:C2:13:73:F9`
- For API and SDK's, select Android/iOS and Web API

# Set Up
Once you've created your app, make sure to save it. Copy and paste your client ID into the app.


You should be good to go! If you run into any issues, feel free to reach out or make an issue on the GitHub