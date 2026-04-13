using Moq;
using Microsoft.Extensions.Logging;
using SunatGreApi.Models;
using SunatGreApi.Models.Dtos;
using SunatGreApi.Repositories;
using SunatGreApi.Services;
using Xunit;

namespace SunatGreApi.Tests
{
    public class GuiaServiceTests
    {
        private readonly Mock<IGuiaRepository> _guiaRepositoryMock;
        private readonly Mock<ISqlServerRepository> _sqlRepositoryMock;
        private readonly Mock<ILogger<GuiaService>> _loggerMock;
        private readonly GuiaService _guiaService;

        public GuiaServiceTests()
        {
            _guiaRepositoryMock = new Mock<IGuiaRepository>();
            _sqlRepositoryMock = new Mock<ISqlServerRepository>();
            _loggerMock = new Mock<ILogger<GuiaService>>();
            _guiaService = new GuiaService(_guiaRepositoryMock.Object, _sqlRepositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessAndRegisterGuiaAsync_DebeRetornarNull_CuandoEstadoEsBaja()
        {
            // Arrange
            var dto = new SunatGreDto { Id = "1", DesEstado = "BAJA" };

            // Act
            var result = await _guiaService.ProcessAndRegisterGuiaAsync(dto);

            // Assert
            Assert.Null(result);
            _guiaRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Guia>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAndRegisterGuiaAsync_DebeRetornarNull_CuandoContieneTwill()
        {
            // Arrange
            var dto = new SunatGreDto 
            { 
                Id = "1", 
                DesEstado = "ACTIVO",
                Traslado = new TrasladoDto
                {
                    Bien = new List<BienDto> 
                    { 
                        new BienDto { DesBien = "TELA TWILL AZUL" } 
                    }
                }
            };

            // Act
            var result = await _guiaService.ProcessAndRegisterGuiaAsync(dto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ProcessAndRegisterGuiaAsync_DebeLanzarExcepcion_CuandoYaExiste()
        {
            // Arrange
            var dto = new SunatGreDto { Id = "1", DesEstado = "ACTIVO" };
            _guiaRepositoryMock.Setup(r => r.ExistsByIdAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _guiaService.ProcessAndRegisterGuiaAsync(dto));
        }

        [Fact]
        public async Task UpdateEstadoProcesoAsync_DebeRetornarFalse_CuandoEstadoInvalido()
        {
            // Act
            var result = await _guiaService.UpdateEstadoProcesoAsync("1", "INVALIDO");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateEstadoProcesoAsync_DebeRetornarTrue_CuandoEstadoValidoYExiste()
        {
            // Arrange
            var guiaId = "1";
            _guiaRepositoryMock.Setup(r => r.GetByIdAsync(guiaId)).ReturnsAsync(new Guia { Id = guiaId });

            // Act
            var result = await _guiaService.UpdateEstadoProcesoAsync(guiaId, "COMPLETADO");

            // Assert
            Assert.True(result);
            _guiaRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Guia>()), Times.Once);
        }
    }
}
