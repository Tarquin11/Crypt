using Crypt.Core.Puzzles;

namespace Crypt.Core.Tests;

public sealed class LevelChallengeCatalogTests
{
    [Fact]
    public void Generated_math_challenge_has_four_answers_one_attempt_and_a_difficulty_timer()
    {
        var math = LevelChallengeCatalog.CreateLevelThreeMathChallenge(new Random(42));

        Assert.Equal(ChallengeTopic.Math, math.Topic);
        Assert.Equal(4, math.Answers.Count);
        Assert.Equal(1, math.MaximumAttempts);
        Assert.NotNull(math.TimeLimit);
        var allowedLimits = new[] { TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10) };
        Assert.True(allowedLimits.Contains(math.TimeLimit.Value));
        Assert.InRange(math.CorrectAnswerIndex, 0, 3);
    }

    [Fact]
    public void Future_cipher_levels_include_a_vigenere_challenge()
    {
        Assert.True(LevelChallengeCatalog.TryGet(2, out var challenge));
        var cipher = Assert.IsType<TextCipherChallenge>(challenge);

        Assert.Equal(CipherMethod.Vigenere, cipher.Method);
        Assert.Equal("ATTACK AT DAWN", cipher.PlainText);
        Assert.Equal(VigenereCipher.Encode(cipher.PlainText, cipher.Key), cipher.EncodedText);
        Assert.NotEmpty(cipher.Hint);
    }
}
