# Shrekbot

![Alt text](ShrekBot%20-%20Net%20Core%203/shrekbot%20pfp.png?raw=true "Shrekbot")

# Description
Discord bot designed after the famous orge.

Mainly created because a friend asked me for it in our private discord server. But over time, I expanded on it as a learning exercise. Therefore, some features are... not entirely designed with Shrek's purpose in mind.

# Commands
## Direct
#### Prefix: ?
* **swamp**
   * `WHAT ARE YOU DOING ON MY SWAMP!?!?!` 
* **donkey**
  * `DONKEY!!!` 
* **random**
  * Responds with a direct quote from the movies
* **help**

### Note: Most commands are owner only and not worth mentioning here
## Indirect
1. __The bot scans each new post for certain key words. If found, it will reply with the appropriate GIF__

### Example

User: `shrekbot when?`

Bot: `https://tenor.com/view/abell46s-reface-shrek-ilucion-optica-screaming-gif-19071434`

The user had "shrek" in the string. The bot detected it and replys with that specific GIF in kind

2. __Every day at a specific time, the bot will post a direct quote from the movies__
3. __Shrek keeps track of certain web link infomation that is posted in a few Discord text channels of our server.__

At the moment those links are:
* YouTube
* Reddit
* Twitter
* TODO: Bluesky

The links are posted to the SQLite database. If the same link was posted by the same or different user, then Shrek will respond to that user and point out the
last time(s) the user posted the same link

4. __The same is done for Discord Attachments but unlike web links, this functionality is expanded to all channels the bot has access and permissions to
If a user posts an image already posted before, then Shrek will respond and call out that user.__

The images are hashed as a 64-bit integer and stored in the database. The Hamming Distance is used for checking for hash similarity.

Shrek will automatically delete blacklisted images if detected and nothing will be inserted into the database.

Likewise, whitelisted images will not trigger a Shrek response, nor be inserted into the database.

## TO DO:

* 64 bit video hashing
* Join a Discord Voice Channel via command to play *Shrek's Theme* and/or *I Need a Hero*

# Frameworks/Libaries used

* Discord.NET v3.6.1
* .NET 6.0
* [ImageSharp v3.1.12](https://github.com/SixLabors/ImageSharp)
  * [ImageHash](https://github.com/coenm/ImageHash/)
* [Dapper v2.1.66](https://github.com/DapperLib/Dapper)
  * [SQLite](https://sqlite.org/index.html) 
* [FileTypeChecker v4.3.0](https://github.com/AJMitev/FileTypeChecker)
* [NewtonsoftJSON v13.0.1](https://www.newtonsoft.com/json)
