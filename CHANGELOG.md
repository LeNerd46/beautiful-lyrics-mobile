# Change Log
All notable changes to this project will be documented in this file.

Heavily based on [beautiful-lyrics](https://github.com/surfbryce/beautiful-lyrics) by [@surfbryce](https://github.com/surfbryce)

## [1.0.0] - 2025-5-
Full release!

### Added

### Fixed
- Background actually darkens now

### Changed

## [1.0.0-preview] - 2025-4-25
Complete rewrite of the app, almost everything is different. This version focused **heavily** on stability instead of new features

### Added
- Heart button displays correct state, you can like and unlike songs
- Line by line lyrics
- Static lyrics

### Fixed
- Songs **never** crash when changing songs
- Songs are **always** in time with Spotify
- Songs **almost always** load instantly

### Changed
- Home screen has been removed
- Album/playlist views have been removed
- Search page has been removed
- Player view has been removed, only lyrics view now
- Onboarding screen is completely different. It has gradient colors and you can actually look at the text
- Can no longer seek in the song, lyrics do not update accordingly anymore
- Lyrics do not update after leaving the app
- Line scrolling is much more efficient

## [0.2] - 2025-1-16
  
Made the app more usuable and (somewhat) pleasent to look at
 
### Added
 - Added content to the home page instead of the "Hello World" stuff
 - Added album and playlist views
 - Added/fixed interludes
 - Words now glow when being sung
 - Added a context menu when long pressing on a song

### Fixed
- Seeking and pausing work (for the most part)
- Really long songs are no longer laggy

### Changed
 - Interludes now have margins consistent with lines
 - Removed the scroll bar from the lyrics view
 - Updated now playing view
 - Lines now return to their normal color after being sung isntead of staying white
 - Adjusted background view
 - Moved lyrics view away from its own page, it is now apart of the home screen
 - Cleaned up the styles XAML file
 - Made the active line more obvious (hopefully?)
 - Switching songs more consistently works (not always, but usually)
 
## [0.1] - 2024-12-16
 Initial Release
 
### Added
  - Basic syllable by syllable/karaoke lyrics
  - Basic custom syncing capabilities