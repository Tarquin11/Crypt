namespace Crypt.Core.Engine;

/// <summary>Framework-independent paged dialogue with a typewriter reveal.</summary>
public sealed class DialogueState
{
    private static readonly TimeSpan CharacterDelay = TimeSpan.FromMilliseconds(24);

    private IReadOnlyList<string> _pages = Array.Empty<string>();
    private int _pageIndex;
    private int _revealedCharacters;
    private TimeSpan _lastCharacterAt;
    private bool _hasCharacterClock;

    public string Speaker { get; private set; } = string.Empty;

    public bool IsVisible => _pages.Count > 0;

    public string CurrentPage => IsVisible ? _pages[_pageIndex] : string.Empty;

    public string VisibleText => CurrentPage[..Math.Min(_revealedCharacters, CurrentPage.Length)];

    public bool IsCurrentPageComplete => !IsVisible || _revealedCharacters >= CurrentPage.Length;

    public void Show(string? speaker, params string?[] pages)
    {
        var usablePages = pages
            .Where(page => !string.IsNullOrWhiteSpace(page))
            .Select(page => page!.Trim())
            .ToArray();

        if (usablePages.Length == 0)
        {
            return;
        }

        Speaker = string.IsNullOrWhiteSpace(speaker) ? "DUNGEON" : speaker.Trim();
        _pages = usablePages;
        _pageIndex = 0;
        _revealedCharacters = 0;
        _lastCharacterAt = TimeSpan.Zero;
        _hasCharacterClock = false;
    }

    public void Clear()
    {
        Speaker = string.Empty;
        _pages = Array.Empty<string>();
        _pageIndex = 0;
        _revealedCharacters = 0;
        _lastCharacterAt = TimeSpan.Zero;
        _hasCharacterClock = false;
    }

    public void Update(TimeSpan now)
    {
        if (!IsVisible || IsCurrentPageComplete)
        {
            return;
        }

        if (!_hasCharacterClock)
        {
            _lastCharacterAt = now;
            _hasCharacterClock = true;
            return;
        }

        var charactersToReveal = (long)((now - _lastCharacterAt).Ticks / CharacterDelay.Ticks);
        if (charactersToReveal <= 0)
        {
            return;
        }

        _revealedCharacters = (int)Math.Min((long)CurrentPage.Length, _revealedCharacters + charactersToReveal);
        _lastCharacterAt += TimeSpan.FromTicks(charactersToReveal * CharacterDelay.Ticks);
    }

    public void Advance()
    {
        if (!IsVisible)
        {
            return;
        }

        if (!IsCurrentPageComplete)
        {
            _revealedCharacters = CurrentPage.Length;
            return;
        }

        if (_pageIndex < _pages.Count - 1)
        {
            _pageIndex++;
            _revealedCharacters = 0;
            _lastCharacterAt = TimeSpan.Zero;
            _hasCharacterClock = false;
            return;
        }

        Clear();
    }
}