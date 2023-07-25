﻿
using System.Globalization;
using JJMasterData.Commons.Configuration.Options;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JJMasterData.Commons.Test.Localization;

public class StringLocalizerTest
{
    [Fact]
    public void StringLocalizerIndexerTest()
    {
        // Arrange
        var localizationOptions = new Mock<IOptions<LocalizationOptions>>();
        var logger = new Mock<ILoggerFactory>();
        var resourceManagerStringLocalizerFactory = new ResourceManagerStringLocalizerFactory(localizationOptions.Object,logger.Object);
        var entityRepository = new Mock<IEntityRepository>();
        var cache = new Mock<IMemoryCache>();
        var options = new Mock<IOptions<JJMasterDataCommonsOptions>>();

        // Act
        var stringLocalizerFactory = new JJMasterDataStringLocalizerFactory(
            resourceManagerStringLocalizerFactory,
            entityRepository.Object,
            cache.Object,
            options.Object);

        // Assert
        var stringLocalizer = stringLocalizerFactory.Create(typeof(JJMasterDataResources));
        Assert.NotNull(stringLocalizer);

        Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-br");
        
        Assert.Equal("Objeto",stringLocalizer["Object"]);
    }

}