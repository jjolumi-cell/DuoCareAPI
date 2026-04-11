using DuoCare.Data;
using DuoCare.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DuoCare.Services
{
    public class AppointmentCancelService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppointmentCancelService> _logger;

        public AppointmentCancelService(IServiceProvider serviceProvider, ILogger<AppointmentCancelService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AppointmentCancelService iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                await CheckExpiredAppointments();
            }
        }

        private async Task CheckExpiredAppointments()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.Now;

                var expiredAppointments = await context.Appointments
                    .Where(a =>
                        a.Date.AddMinutes(20) < now &&
                        a.Status != AppointmentStatus.Completado &&
                        a.Status != AppointmentStatus.Cancelado)
                    .ToListAsync();

                _logger.LogInformation("Citas revisadas: {Count}", expiredAppointments.Count);

                if (!expiredAppointments.Any())
                    return;

                foreach (var appointment in expiredAppointments)
                {
                    _logger.LogWarning("Cita cancelada automáticamente: {AppointmentId}, usuario ausente: {AbsentUserId}", appointment.Id, appointment.SenderId);

                    appointment.AbsentUserId = appointment.SenderId;
                    appointment.AbsentUserLatitude = null;
                    appointment.AbsentUserLongitude = null;
                    appointment.AbsentUserDistance = null;

                    appointment.Status = AppointmentStatus.Cancelado;
                    appointment.AutoCancelledAt = DateTime.Now;
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Citas canceladas: {Count}", expiredAppointments.Count);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al guardar citas canceladas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar citas canceladas");
            }
        }
    }
}

