using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ReverseSentence.DTOs;
using ReverseSentence.Models;
using ReverseSentence.Repositories;
using ReverseSentence.Services;

namespace ReverseSentence.Tests.Services;

public class ReverseServiceTests
{
    private readonly Mock<IReverseRepository> mockRepository;
    private readonly Mock<ICache> mockCache;
    private readonly Mock<ICurrentUserService> mockCurrentUserService;
    private readonly Mock<ILogger<ReverseService>> mockLogger;
    private readonly ReverseService service;

    public ReverseServiceTests()
    {
        mockRepository = new Mock<IReverseRepository>();
        mockCache = new Mock<ICache>();
        mockCurrentUserService = new Mock<ICurrentUserService>();
        mockLogger = new Mock<ILogger<ReverseService>>();

        service = new ReverseService(
            mockRepository.Object,
            mockCache.Object,
            mockCurrentUserService.Object,
            mockLogger.Object
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReverseService(
            null!,
            mockCache.Object,
            mockCurrentUserService.Object,
            mockLogger.Object
        );

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReverseService(
            mockRepository.Object,
            null!,
            mockCurrentUserService.Object,
            mockLogger.Object
        );

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullCurrentUserService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReverseService(
            mockRepository.Object,
            mockCache.Object,
            null!,
            mockLogger.Object
        );

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("currentUserService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReverseService(
            mockRepository.Object,
            mockCache.Object,
            mockCurrentUserService.Object,
            null!
        );

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region ReverseWordsAsync Tests

    [Fact]
    public async Task ReverseWordsAsync_WithValidSentence_ShouldReverseWords()
    {
        // Arrange
        var sentence = "Hello World";
        var expectedReversed = "olleH dlroW";
        var userId = "user123";

        mockCurrentUserService.Setup(x => x.GetUserId()).Returns(userId);
        mockCache.Setup(x => x.GetAsync<ReverseResponseDto>(It.IsAny<string>()))
            .ReturnsAsync((ReverseResponseDto?)null);

        // Act
        var result = await service.ReverseWordsAsync(sentence);

        // Assert
        result.Should().NotBeNull();
        result.OriginalSentence.Should().Be(sentence);
        result.ReversedSentence.Should().Be(expectedReversed);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        mockRepository.Verify(x => x.CreateAsync(It.Is<ReverseRequest>(r =>
            r.UserId == userId &&
            r.OriginalSentence == sentence &&
            r.ReversedSentence == expectedReversed
        )), Times.Once);

        mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<ReverseResponseDto>(),
            It.Is<TimeSpan>(ts => ts == TimeSpan.FromHours(24))
        ), Times.Once);
    }

    [Fact]
    public async Task ReverseWordsAsync_WithCachedResult_ShouldReturnCachedValue()
    {
        // Arrange
        var sentence = "Hello World";
        var userId = "user123";
        var cachedResponse = new ReverseResponseDto
        {
            OriginalSentence = sentence,
            ReversedSentence = "olleH dlroW",
            Timestamp = DateTime.UtcNow
        };

        mockCurrentUserService.Setup(x => x.GetUserId()).Returns(userId);
        mockCache.Setup(x => x.GetAsync<ReverseResponseDto>(It.IsAny<string>()))
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await service.ReverseWordsAsync(sentence);

        // Assert
        result.Should().BeEquivalentTo(cachedResponse);
        mockRepository.Verify(x => x.CreateAsync(It.IsAny<ReverseRequest>()), Times.Never);
        mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<ReverseResponseDto>(),
            It.IsAny<TimeSpan>()
        ), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReverseWordsAsync_WithNullOrWhitespaceSentence_ShouldThrowArgumentException(string? sentence)
    {
        // Act & Assert
        var act = async () => await service.ReverseWordsAsync(sentence!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sentence");
    }

    [Fact]
    public async Task ReverseWordsAsync_WithMultipleSpaces_ShouldPreserveSpaces()
    {
        // Arrange
        var sentence = "Hello  World";
        var expectedReversed = "olleH  dlroW";
        var userId = "user123";

        mockCurrentUserService.Setup(x => x.GetUserId()).Returns(userId);
        mockCache.Setup(x => x.GetAsync<ReverseResponseDto>(It.IsAny<string>()))
            .ReturnsAsync((ReverseResponseDto?)null);

        // Act
        var result = await service.ReverseWordsAsync(sentence);

        // Assert
        result.ReversedSentence.Should().Be(expectedReversed);
    }

    [Fact]
    public async Task ReverseWordsAsync_WithSingleWord_ShouldReverseWord()
    {
        // Arrange
        var sentence = "Hello";
        var expectedReversed = "olleH";
        var userId = "user123";

        mockCurrentUserService.Setup(x => x.GetUserId()).Returns(userId);
        mockCache.Setup(x => x.GetAsync<ReverseResponseDto>(It.IsAny<string>()))
            .ReturnsAsync((ReverseResponseDto?)null);

        // Act
        var result = await service.ReverseWordsAsync(sentence);

        // Assert
        result.ReversedSentence.Should().Be(expectedReversed);
    }

    #endregion

    #region GetHistoryPagedAsync Tests

    [Fact]
    public async Task GetHistoryPagedAsync_WithValidParameters_ShouldReturnPagedResponse()
    {
        // Arrange
        var userId = "user123";
        var page = 1;
        var pageSize = 10;
        var totalCount = 25L;

        var mockData = new List<ReverseRequest>
        {
            new() { Id = "1", UserId = userId, OriginalSentence = "Hello", ReversedSentence = "olleH", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", UserId = userId, OriginalSentence = "World", ReversedSentence = "dlroW", CreatedAt = DateTime.UtcNow }
        };

        mockCurrentUserService.Setup(x => x.GetUserId()).Returns(userId);
        mockRepository.Setup(x => x.GetPagedAsync(userId, page, pageSize))
            .ReturnsAsync((mockData, totalCount));

        // Act
        var result = await service.GetHistoryPagedAsync(page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.CurrentPage.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().Be((int)totalCount);
        result.TotalPages.Should().Be(3);
        result.Data.Should().HaveCount(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetHistoryPagedAsync_WithInvalidPage_ShouldThrowArgumentException(int page)
    {
        // Act & Assert
        var act = async () => await service.GetHistoryPagedAsync(page, 10);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetHistoryPagedAsync_WithInvalidPageSize_ShouldThrowArgumentException(int pageSize)
    {
        // Act & Assert
        var act = async () => await service.GetHistoryPagedAsync(1, pageSize);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("pageSize");
    }

    [Fact]
    public async Task GetHistoryPagedAsync_WithLastPage_ShouldNotHaveNext()
    {
        // Arrange
        var userId = "user123";
        var page = 3;
        var pageSize = 10;
        var totalCount = 25L;

        var mockData = new List<ReverseRequest>
        {
            new() { Id = "1", UserId = userId, OriginalSentence = "Hello", ReversedSentence = "olleH", CreatedAt = DateTime.UtcNow }
        };

        mockCurrentUserService.Setup(x => x.GetUserId()).Returns(userId);
        mockRepository.Setup(x => x.GetPagedAsync(userId, page, pageSize))
            .ReturnsAsync((mockData, totalCount));

        // Act
        var result = await service.GetHistoryPagedAsync(page, pageSize);

        // Assert
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    #endregion

    #region SearchByWordAsync Tests

    [Fact]
    public async Task SearchByWordAsync_WithValidWord_ShouldReturnResults()
    {
        // Arrange
        var userId = "user123";
        var searchWord = "Hello";

        var mockData = new List<ReverseRequest>
        {
            new() { Id = "1", UserId = userId, OriginalSentence = "Hello World", ReversedSentence = "olleH dlroW", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", UserId = userId, OriginalSentence = "Say Hello", ReversedSentence = "yaS olleH", CreatedAt = DateTime.UtcNow }
        };

        mockCurrentUserService.Setup(x => x.GetUserId()).Returns(userId);
        mockRepository.Setup(x => x.SearchByWordAsync(userId, searchWord))
            .ReturnsAsync(mockData);

        // Act
        var result = await service.SearchByWordAsync(searchWord);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(item =>
        {
            item.Id.Should().NotBeNullOrEmpty();
            item.OriginalSentence.Should().NotBeNullOrEmpty();
            item.ReversedSentence.Should().NotBeNullOrEmpty();
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchByWordAsync_WithNullOrWhitespaceWord_ShouldThrowArgumentException(string? word)
    {
        // Act & Assert
        var act = async () => await service.SearchByWordAsync(word!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("word");
    }

    [Fact]
    public async Task SearchByWordAsync_WithWordTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longWord = new string('a', 101);

        // Act & Assert
        var act = async () => await service.SearchByWordAsync(longWord);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("word")
            .WithMessage("*too long*");
    }

    [Fact]
    public async Task SearchByWordAsync_WithWhitespace_ShouldTrimWord()
    {
        // Arrange
        var userId = "user123";
        var searchWord = "  Hello  ";
        var trimmedWord = "Hello";

        mockCurrentUserService.Setup(x => x.GetUserId()).Returns(userId);
        mockRepository.Setup(x => x.SearchByWordAsync(userId, trimmedWord))
            .ReturnsAsync(new List<ReverseRequest>());

        // Act
        await service.SearchByWordAsync(searchWord);

        // Assert
        mockRepository.Verify(x => x.SearchByWordAsync(userId, trimmedWord), Times.Once);
    }

    #endregion
}
