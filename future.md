# Future Plans

This is a place for me to organize my ideas and share my plans for the future. If you have any ideas of your own, or you have something you want to add or refine about one of the ideas listed below, feel free to create an issue and share your idea

## Custom Syncing

### Overview

If you have a song that you really like, but it doesn't have beat by beat lyrics, why not sync it yourself? This was actually the main reason I wanted to make this whole project. That's a whole other story, but I later had the idea to just make beautiful lyrics on mobile, which is now the main focus.

### How Would It Work?

You would have to go through the song twice, once for the lead vocals, then another time for the backing vocals

- **Lead Vocals**
  - Split each word (if needed)
  - Listen to the song, tap and hold when the word is sung, release once it's done being sung
- **Backing Vocals**
  - Same system as lead vocals, tap and hold then release
  - Probably skip to a little bit before where the backing vocals start, so you don't have to listen to the whole song again
  - Add ability to add your own backing vocals, in case Apple didn't include the lyrics themselves
- **Sharing**
  - Once you finish syncing your song, you can upload the lyrics to the server
  - Then we will have an even larger library of synced songs, first it will check from Apple, and if it only has line by line, then it will check our servers

## Music Timeline

### Overview

Maybe this isn't important to you, but for me it totally is. If you either don't care or don't trust it or whatever, it'll be completely optional. But pretty much, you can upload your Spotify listening history and/or link your Last.fm account and it will go through and analyze your entire music listening history. Last.fm does a great job at this already, but it's definitely clunky to use sometimes and overall, the insights could be better. I'm not looking to replace Last.fm, just simply have another way to view it in a Spotify Wrapped-like manner

There's also a non-zero chance that this is just a me thing, but I've had some pretty distinct listening time periods. Those time periods were very important to me, and seeing those songs again could invoke those important memories. I believe that this could be incredibly valuable, but if you don't think so, maybe it'll at least reconnect you with a song you totally forgot about

### How Would It Work?

- Download your Spotify listening history and/or link your Last.fm account. It will use Last.fm for more recent things, the Spotify listening history is for everything before you created you Last.fm account
- The timeline will group your music listening groups together. Did you listen to a lot of one artist in December and January, but completely moved on after that? It will show you that in December and January you listened to that artist, along with any other groups it can identify
- The timeline will group any songs, albums, or artists. If you listened to a lot of just one artist, it will display that artist instead their individual songs. But if you listened to a lot of random songs, it will group all of those songs together
- You can also look into specific songs. Let's say you listened to a song a lot a year ago, and again a couple of months ago. You would be able to look into that song specifically and get insights on it or something like that
- Potentially analyze the songs themselves that you were listening to and provide what those songs mean. Or, if they don't really have any correlation, then don't provide an analysis for it
  - For example, if you were listening to a lot of breakup songs, perhaps you were... going through a breakup and it could point that out. Not in a mean way, of course
- You will have lots of insights into your listening history, and it can create different playlists for you if you'd like
- Again, this is totally optional. If you would rather wait for wrapped and don't want to see any of your data, that's totally fine, I could totally understand wanting to do that

## Friend System

### Overview

I'm not entirely sure how I want to do this. There's so much potential for this, but it has to be implemented in an easy to use way and most importantly, a fun and not stupid way. The simple act of having friends is not valuable, the whole idea is you can do fun things to interact with your friends, bringing your music into your friendship

Why do I think this is important? Me and my friends had the .fmbot in our Discord server a while back, and it brought music into our friendships. I didn't realize it at the time, but that was just another fun way to bond together. Music is a large part of lots of people's lives, but beyond sharing music suggestions, it doesn't really make its way into friendships. .fmbot had a crown system, and me and my friends had lots of fun competing for the crown on the artists we listened to. I find simple interactions like that very valuable, and I think there's a lot you could do with things like that

### How Would It Work?

- Again, there's tons of potential here, it's just a matter of coming up with ideas
- Listening competitions could be fun, where you compete for the most amount of listens on an artist, album, or song
- There's a lot more ideas, but I would need feedback to see if it's something people would actually want (sending songs to friends, challenges of some sort, special playlists where you can add notes between songs or just the ability to add notes to songs I guess, etc.)

## Karaoke

### Overview

Man, I've been thinking about this a lot. I don't think it's really possible, but it would be really cool if it was. Apple Music has this thing where you can lower the volume of the vocals, it's called Apple Music Sing. So like, it's obviously possible, but from a technical standpoint, I'm not sure it's really possible on Android, or at least .NET MAUI (the framework I'm using). I don't want to get too technical here, because that's not what this is about, but Apple has direct access to their hardware and operating system and whatnot, and I just don't know if I can really access the NPU on Android, let alone make/find a good enough model to split the music like they do in real time. Or maybe it's not in real time, I don't know. Either way, it's very likely not possible, but it would be cool, wouldn't it? Oh also, not to mention, I don't have access to the straight-up audio from Spotify. I'd have to get it from YouTube or something, and that audio could potentially be different from what you're listening to on Spotify. I don't know, maybe we could just have a database of instrumental version of songs or something and use that, it's just ideas is all

> Just to re-emphasize it so that it's clear to you and so that technical people don't think I'm stupid, this is most likely impossible!!!

### Details:

- Control the volume of the vocals just like in Apple Music
- Very important, this is likely never going to actually happen!

## Other Smaller Ideas

### Overview

These are just some very random, smaller ideas. These don't have anywhere near as much thought put into them as the other ones, but just some potential ideas. Honestly pretty unlikely to make it in, but if it gets enough support, then I would add it

- Lyric Stories
  - Add stories, memories, or information behind certain lyrics. You can keep them to yourself or share them with your friends
- Re-liking songs?
  - Pretty much, you can like a song multiple times and you can view each date you liked them
  - I only thought of this because I recently came back to a song that I used to listen to a lot, and I've been listening to a lot again. I guess this would maybe show up in the timeline potentially, but it could be cool to just have a simple way to view each time I liked this song
- Simple music games
  - Fill in the blank lyrics
  - A game involving the top artists, guessing who they are, list a song or album they made, etc.
  - You're not allowed to create a game using Spotify's API, so... only games that don't involve Spotify directly (honestly this whole project kind of bends/breaks the rules with their API)
- Lyrics Insights
  - Have something to explain the meaning behind the lyrics. Not sure how that would work, as I haven't looked into it, but I'm sure there's some sort of API out there for that
