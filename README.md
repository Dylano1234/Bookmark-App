## Bookmark App

A desktop application for managing bookmarks and personal collections of media, such as manga, books, and TV shows.

The application allows users to organize their collections into lists, track their progress, assign ratings, and manage additional information about individual items.

It is developed using C# and WPF, with SQLite for local data storage and Google Drive integration for cloud synchronization.

## Features
- Collection management: Create and manage lists containing different types of media.
- Progress tracking: Track progress for individual items using numerical values.
- Ratings: Assign ratings to items.
- Genres: Organize items using genres and assign multiple genres to an item.
- Cover images: Store images associated with individual items.
- Additional information: Store information such as release schedules or other user-defined text.
- Local database: Store application data locally using SQLite.
- Cloud synchronization: Synchronize application data between devices using Google Drive.

## Screenshots
### Home view  

<img width="1919" height="1008" alt="image" src="https://github.com/user-attachments/assets/5b131005-ce70-4ec4-8efa-c81ef62fc052" />  

### List view  

<img width="1919" height="1008" alt="image" src="https://github.com/user-attachments/assets/48310435-924f-4490-a551-2d2a2f090fd4" />  

### Detail view  

<img width="1919" height="1007" alt="image" src="https://github.com/user-attachments/assets/0bd41de9-d3c0-4cb8-8982-3904340ae362" />  

## Cloud Synchronization

The application supports synchronization between devices using Google Drive.

Application data is stored locally in an SQLite database and can be synchronized between devices through Google Drive.

The synchronization functionality allows users to maintain their collection across multiple installations of the application.
It is advised to keep only one instance of the application open at a time. If not, you could end up overwriting progress with an older state since the application currently only checks for new data once on startup.

A Google account is required to use the cloud synchronization functionality.

Note: Synchronization requires an internet connection and access to Google Drive.
