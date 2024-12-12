# Beautiful Lyrics Mobile Setup

This guide will show you how to create a Spotify Developer App and get the necessary information in order for the app to function properly. 


## Why Do You Need to Create a Developer App?
I would love to just have everyone use my own app, but after looking at Spotify's developer policy, there is simply no way that Spotify would accept my request. By default, Spotify Developer Apps can only have up to 25 invited users use the app. To allow anyone to use the app, you must submit a request to Spotify where they will then review your app. It is just highly unlikely that they will accept it

# Creating the App

* Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
* Create an app
* For the redirect URL's, add http://localhost:5543/callback
* Spotify requires the Android app package name. The package name is `com.lenerd46.beautifullyrics` and the SHA1 fingerprint is `FD:9C:96:42:D8:F5:D7:EB:F0:8A:CE:F1:47:3D:D5:F9:C2:13:73:F9`
* For API and SDK's, select Android/iOS and Web API

# Set Up
Once you've created your app, make sure to save it. Copy and paste your client ID and secret into the app. The secret is needed for the web API


You should be good to go! If you run into any issues, feel free to reach out or make an issue on the GitHub