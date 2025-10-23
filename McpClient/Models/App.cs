using System;
using System.Collections.Generic;

namespace McpClient.Models;

internal class StoreApp
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }
    public string AppName { get; set; }
    public string AppDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public User User { get; set; }
    public StoreInfo StoreInfo { get; set; }
    public Usage Usage { get; set; }
    public Feedback Feedback { get; set; }
    public Permissions Permissions { get; set; }
    public List<string> Tags { get; set; }
    public int Status { get; set; }
    public string AppCode { get; set; }
    public int VersionNumber { get; set; }
    public string VersionLabel { get; set; }
    public AppPrompt AppPrompt { get; set; }
    public List<Changelog> Changelogs { get; set; }
    public List<Review> Reviews { get; set; }
    public List<StoreMedia> StoreMedias { get; set; }
}

internal class AppPrompt
{
    public string Prompt { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
}

internal class Changelog
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string App { get; set; }
    public string Version { get; set; }
    public DateTime Date { get; set; }
    public List<string> Changes { get; set; }
    public List<string> BugFixes { get; set; }
    public List<string> Improvements { get; set; }
}

internal class Feedback
{
    public int Rating { get; set; }
    public int TotalRatings { get; set; }
}

internal class Permissions
{
    public bool FileAccess { get; set; }
    public bool NetworkAccess { get; set; }
    public bool DatabaseAccess { get; set; }
    public bool CodeExecution { get; set; }
    public bool AllowCommercialUse { get; set; }
    public bool RequiredAttribution { get; set; }
}

internal class Reply
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string Review { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string UserName { get; set; }
    public string UserAvatar { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal class Review
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string App { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string UserName { get; set; }
    public string UserAvatar { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public List<string> Pros { get; set; }
    public List<string> Cons { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Helpful { get; set; }
    public List<Reply> Replies { get; set; }
}

internal class StoreInfo
{
    public int Category { get; set; }
    public int ToolCount { get; set; }
    public int RequiredTokens { get; set; }
    public int ResourceLevel { get; set; }
    public string SupportedPlatforms { get; set; }
    public int Price { get; set; }
    public string License { get; set; }
    public int DownloadCount { get; set; }
}

internal class StoreMedia
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string MediaType { get; set; }
    public string MediaData { get; set; }
    public string FileHash { get; set; }
    public string MimeType { get; set; }
}

internal class Usage
{
    public int TotalExecutions { get; set; }
    public DateTime LastExecuted { get; set; }
    public int SuccessRate { get; set; }
}

internal class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public string Dn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

internal enum AppStoreSortBy
{
    updatedAt,
    createdAt,
    name,
    downloadCount,
    rating
}

internal enum AppStoreOrder
{
    desc,
    asc
}