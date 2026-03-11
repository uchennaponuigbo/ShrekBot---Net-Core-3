using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using FileTypeChecker;
using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using ShrekBot.Modules.Data_Files_and_Management.Database;
using ShrekBot.Modules.Swamp.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Text;

namespace ShrekBot
{
    internal class ImageComparison
    {
        private static ConcurrentDictionary<ulong, string> _abominations; //key is the hash, value is the hash.ToString()
        private static ConcurrentDictionary<ulong, string> _exempt; //hashes that are also not to be put in database
        private static ConcurrentDictionary<ulong, string> _falsePositives;
        //private DifferenceHash _differenceHash;

        //private const double _HammingDistanceTolerance = 90.0; //Anything less than this value is assumed not to be the abomination.
        //private const ulong _KirbeeDiffHash = 12729610065190879872;

        internal ImageComparison()
        {
            //_abominations = new ConcurrentDictionary<ulong, string>();
            //_exempt = new ConcurrentDictionary<ulong, string>();
            //_falsePositives = new ConcurrentDictionary<ulong, string>();

            ////_differenceHash = new DifferenceHash();

            //FileTypeValidator.RegisterCustomTypes(typeof(CFT_Mov).Assembly);
            
            ////variants of the abomination hash that I will check in case Drake does anything to circumvent
            ////and this list will be updated in case he does stuff
            //_abominations.GetOrAdd(12729610065190879872, "kirbee.png");
            //_abominations.GetOrAdd(17361654623738299008, "kirbeeDimmed.png or kirbeeBright.png"); 
            //_abominations.GetOrAdd(7521223104841945222, "kirbeeLeft.png"); 
            //_abominations.GetOrAdd(11454614242821208168, "kirbeeRight.png"); 
            //_abominations.GetOrAdd(1052981330386119282, "kirbeeUpsideDown.png"); 
            //_abominations.GetOrAdd(1052981330386119218, "kirbeeUpsideDownDimmed.png"); 
            //_abominations.GetOrAdd(12729610065190879872, "kirbee_blurred.jpg"); 
            //_abominations.GetOrAdd(17357129016430085256, "kirbeeForest.jpg"); 
            //_abominations.GetOrAdd(12792664600322540160, "kirbeeSprings.jpg");

            //_exempt.GetOrAdd(6324954117335690628, "ddd.jpg"); 
        }

        static ImageComparison()
        {
            _abominations = new ConcurrentDictionary<ulong, string>();
            _exempt = new ConcurrentDictionary<ulong, string>();
            _falsePositives = new ConcurrentDictionary<ulong, string>();

            FileTypeValidator.RegisterCustomTypes(typeof(CFT_Mov).Assembly);

            //variants of the abomination hash that I will check in case Drake does anything to circumvent
            //and this list will be updated in case he does stuff
            _abominations.GetOrAdd(12729610065190879872, "kirbee.png");
            _abominations.GetOrAdd(17361654623738299008, "kirbeeDimmed.png or kirbeeBright.png");
            _abominations.GetOrAdd(7521223104841945222, "kirbeeLeft.png");
            _abominations.GetOrAdd(11454614242821208168, "kirbeeRight.png");
            _abominations.GetOrAdd(1052981330386119282, "kirbeeUpsideDown.png");
            _abominations.GetOrAdd(1052981330386119218, "kirbeeUpsideDownDimmed.png");
            _abominations.GetOrAdd(12729610065190879872, "kirbee_blurred.jpg");
            _abominations.GetOrAdd(17357129016430085256, "kirbeeForest.jpg");
            _abominations.GetOrAdd(12792664600322540160, "kirbeeSprings.jpg");

            _exempt.GetOrAdd(6324954117335690628, "ddd.jpg");
        }

        internal ulong DifferenceHash(Stream streamFromAttachmentURL)
        {
            streamFromAttachmentURL.Position = 0;
            ulong hash = 0;
            DifferenceHash _differenceHash = new DifferenceHash();
            using (Image<Rgba32> imageSource = Image.Load<Rgba32>(streamFromAttachmentURL))
                hash = _differenceHash.Hash(imageSource);
            return hash;
        }

        /// <summary>
        /// Checks if the passed in hash closely matches the original hash abomination
        /// </summary>
        /// <param name="hashedPercentage"></param>
        /// <returns> <c>True</c> if the percentage is greater than <c>90.0</c>. A higher percentage closely matches the original image</returns>
        internal bool CheckHashSimilarityToAbomination(ulong hash)
        {//_KirbeeDiffHash = 12729610065190879872
            const double _HammingDistanceTolerance = 90.0;
            double hashedPercentage = Math.Round(CompareHash.Similarity(12729610065190879872, hash), 2);
            return hashedPercentage >= _HammingDistanceTolerance;
        }

        //TODO: Complete this
        internal ulong VideoHash(Stream streamFromAttachmentURL)
        {
            return 0;
        }

        /// <summary>
        /// The variant that Drake made is added to the blacklist, while all instances are removed from the database
        /// </summary>
        /// <remarks>The database removal is needed because there is no intention of an abomination being DDD meme'd</remarks>
        /// <returns>The number of records deleted from database</returns>
        internal int AddAbominationVariant(ulong diffHashVariant, string name)
        {
            if(!IsThisVariantOfAbomination(diffHashVariant))
            {
                _abominations.GetOrAdd(diffHashVariant, name);
                //delete hash from Image Database, if it exists
                return SwampDB.DeleteAbominationVariantFrom_Images(diffHashVariant);
            }
            return 0;
        }

        /// <summary>
        /// Does this hash exist within the list of abomination hashes
        /// </summary>
        /// <param name="abominationHash"></param>
        /// <returns><c>true</c> if yes, <c>false</c> if not.</returns>
        internal bool IsThisVariantOfAbomination(ulong abominationHash)
            => _abominations.ContainsKey(abominationHash);

        /// <summary>
        /// Removes an abomination variant from the blacklist, not database
        /// </summary>
        /// <remarks>Why would I ever call this function directly or indirectly?</remarks>
        /// <param name="abominationHash"></param>
        /// <param name="swamp"></param>
        /// <returns><c>true</c> if successfully removed. <c>false</c> if unable to.</returns>
        internal bool RemoveAbominationVariant(ulong abominationHash)
            => _abominations.TryRemove(abominationHash, out string _);
        
        /// <summary>
        /// Gets a string of hashes and filenames of the hashes from the abomination blacklist
        /// </summary>
        /// <returns></returns>
        internal string GetAbominationKeysAndNames()
            => PrintDictionaries(ref _abominations, "**__The Abomination and its Variants__**");
        
        /// <summary>
        /// Adds an exempt hash and removes all records of it from the database
        /// </summary>
        /// <param name="exemptHash"></param>
        /// <param name="name"></param>
        /// <returns>The number of records deleted, if it existed in the databasse</returns>
        internal int AddExemptHash(ulong exemptHash, string name)
        {
            if(!IsThisExemptHash(exemptHash))
            {
                _exempt.GetOrAdd(exemptHash, name);
                return SwampDB.DeleteAbominationVariantFrom_Images(exemptHash);
            }//there's always a chance that an exempted image is in the databasse
            return 0;
        }

        /// <summary>
        /// Does this hash exist within the list of exempt hashes
        /// </summary>
        /// <remarks>Exempted images are not to be inserted into the database for reasons like direct replying purposes or geunine false positives</remarks>
        /// <param name="hash"></param>
        /// <returns><c>true</c> if yes, <c>false</c> if not.</returns>
        internal bool IsThisExemptHash(ulong hash)
            => _exempt.ContainsKey(hash);

        /// <summary>
        /// Removes an exempt hash from the dictionary, not database
        /// </summary>
        /// <remarks>Mistakes could be made, hence this method's existence</remarks>
        /// <param name="abominationHash"></param>
        /// <param name="swamp"></param>
        /// <returns><c>true</c> if successfully removed. <c>false</c> if unable to.</returns>
        internal bool RemoveExemptHash(ulong exemptHash)
            => _exempt.TryRemove(exemptHash, out string _);
        
        /// <summary>
        /// Gets a string of hashes and filenames of the hashes from the exempt dictionary
        /// </summary>
        /// <returns></returns>
        internal string GetExemptKeysAndNames() 
            => PrintDictionaries(ref _exempt, "**__Exempt Images__**");

        /// <summary>
        /// Adds a hash value to the list that is neither an abomination variant or an exempt hash
        /// </summary>
        /// <remarks>Values in the list are meant to be added to database after I visually verify that this is a false positive</remarks>
        /// <param name="falsePositiveHash"></param>
        /// <param name="name"></param>
        internal void AddFalsePositive(ulong falsePositiveHash, string name)
        {
            if (!IsThisAFalsePositive(falsePositiveHash))
                _falsePositives.TryAdd(falsePositiveHash, name);
        }

        /// <summary>
        /// Does this exist in the list of false positives that may or may not be abominations?
        /// </summary>
        /// <param name="falsePositiveHash"></param>
        /// <returns></returns>
        internal bool IsThisAFalsePositive(ulong falsePositiveHash)
            => _falsePositives.ContainsKey(falsePositiveHash);

        /// <summary>
        /// Removes a false positive hash from the checklist, not database
        /// </summary>
        /// <remarks>Maybe this wasn't a false positive after all...</remarks>
        /// <param name="falsePositiveHash"></param>
        /// <param name="swamp"></param>
        /// <returns><c>true</c> if successfully removed. <c>false</c> if unable to.</returns>
        internal bool RemoveFalsePositiveHash(ulong falsePositiveHash)
            => _falsePositives.TryRemove(falsePositiveHash, out string _);
        
        internal string GetFalsePositiveKeysAndNames() 
            => PrintDictionaries(ref _falsePositives, "**__False Positives__**");
        
        /// <summary>
        /// Excludes GIFs
        /// </summary>
        /// <param name="image"></param>
        /// <returns><c>Media.Image</c> if true, else <c>Media.None</c></returns>
        internal Media IsAttachmentAnImageType(Stream image)
        { 
            //.jpg, .png, bmp, tiff, heic, webp
            //grabbed internal logic from stream.IsImage() because I don't want to include GIFs
            //https://github.com/AJMitev/FileTypeChecker/blob/master/FileTypeChecker/Extensions/StreamExtensions.cs
            bool x = image.Is<JointPhotographicExpertsGroup>()
                || image.Is<PortableNetworkGraphic>()
            || image.Is<Webp>()
            || image.Is<Bitmap>()    
            || image.Is<TaggedImageFileFormat>();
            if (x)
                return Media.Image;
            return Media.None;
            //return image.IsImage();
            //image.IsImageAsync();
        }

        /// <summary>
        /// Excludes .mkv
        /// </summary>
        /// <param name="video"></param>
        /// <returns><c>Media.Video</c> if true, else <c>Media.None</c></returns>
        internal Media IsAttachmentAVideoType(Stream video)
        {
            //.avi, .mp4, .m4v, .mov
            //not doing mkv because you can't play those files on Discord
            bool y = video.Is<Mp4>() 
                || video.Is<M4V>()
                || video.Is<CFT_Mov>()
                || video.Is<AudioVideoInterleaveVideoFormat>();
            if (y)
                return Media.Video;
            return Media.None;
        }

        private string PrintDictionaries(ref ConcurrentDictionary<ulong, string> dictionary, string title)
        {
            StringBuilder sb = new StringBuilder(title + "\n");
            sb.AppendLine("**Hash** | _Name_");
            foreach (var item in dictionary)
                sb.AppendLine($"**{item.Key}** | _{item.Value}_");
            return sb.ToString();
        }
    }
}
