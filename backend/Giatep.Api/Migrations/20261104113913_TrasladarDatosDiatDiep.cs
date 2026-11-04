using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Giatep.Api.Models;
namespace Giatep.Api.Migrations;
[DbContext(typeof(GiatepDbContext))]
[Migration("20261104113913_TrasladarDatosDiatDiep")]
public class TrasladarDatosDiatDiep : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
migrationBuilder.Sql("""
INSERT INTO [Auditorias] ([UsuarioId],[Actor],[Entidad],[RegistroId],[Accion],[Cambios],[Fecha])
SELECT NULL,N'Migración de formularios',N'DenunciasCasos',CONVERT(nvarchar(80),d.[CasoId]),N'ArchivoNormalizacion',
 (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES),SYSUTCDATETIME()
FROM [DenunciasCasos] d
WHERE NOT EXISTS (SELECT 1 FROM [Auditorias] a WHERE a.[RegistroId]=CONVERT(nvarchar(80),d.[CasoId]) AND a.[Accion]=N'ArchivoNormalizacion');
IF EXISTS (SELECT 1 FROM [DenunciasCasos] d JOIN [Casos] c ON c.[Id]=d.[CasoId]
 WHERE d.[Formulario] NOT IN ('DIAT','DIEP') OR (d.[Formulario]='DIAT' AND c.[Tipo]<>0) OR (d.[Formulario]='DIEP' AND c.[Tipo]<>1))
 THROW 51000,'Existen formularios incompatibles con el tipo del caso. No se ha eliminado ninguna tabla.',1;
UPDATE c SET c.[FechaEmision]=d.[FechaEmision], c.[CodigoOrganismo]=d.[CodigoOrganismo], c.[Folio]=d.[Folio], c.[DenuncianteNombre]=d.[DenuncianteNombre], c.[DenuncianteRun]=d.[DenuncianteRun], c.[DenuncianteTelefono]=d.[DenuncianteTelefono], c.[DenuncianteClasificacion]=d.[DenuncianteClasificacion], c.[FirmaNombre]=d.[FirmaNombre] FROM [Casos] c JOIN [DenunciasCasos] d ON d.[CasoId]=c.[Id];
;WITH ultimo AS (SELECT d.*, ROW_NUMBER() OVER(PARTITION BY d.[TrabajadorId] ORDER BY c.[CreadoEn] DESC,c.[Id] DESC) rn FROM [DenunciasCasos] d JOIN [Casos] c ON c.[Id]=d.[CasoId]) UPDATE t SET t.[FechaIngreso]=d.[FechaIngreso], t.[Antiguedad]=d.[Antiguedad], t.[AntiguedadUnidad]=d.[AntiguedadUnidad], t.[TipoContrato]=d.[TipoContrato], t.[TipoIngreso]=d.[TipoIngreso], t.[CategoriaOcupacional]=d.[CategoriaOcupacional] FROM [Trabajadores] t JOIN ultimo d ON d.[TrabajadorId]=t.[Id] AND d.rn=1;
INSERT INTO [DIAT] ([Id], [CasoId], [HoraAccidente], [HoraIngreso], [HoraSalida], [AccidenteDireccion], [AccidenteComuna], [ActividadAccidente], [LugarAccidente], [TrabajoHabitual], [RealizabaHabitual], [ClasificacionAccidente], [TipoAccidente], [TipoTrayecto], [MedioPrueba], [DetallePrueba]) SELECT [Id], [CasoId], [HoraAccidente], [HoraIngreso], [HoraSalida], [AccidenteDireccion], [AccidenteComuna], [ActividadAccidente], [LugarAccidente], [TrabajoHabitual], [RealizabaHabitual], [ClasificacionAccidente], [TipoAccidente], [TipoTrayecto], [MedioPrueba], [DetallePrueba] FROM [DenunciasCasos] WHERE [Formulario]='DIAT';
IF (SELECT COUNT(*) FROM [DIAT])<>(SELECT COUNT(*) FROM [DenunciasCasos] WHERE [Formulario]='DIAT') THROW 51001,'No coinciden los registros de DIAT.',1;
IF EXISTS (SELECT [Id], [CasoId], [HoraAccidente], [HoraIngreso], [HoraSalida], [AccidenteDireccion], [AccidenteComuna], [ActividadAccidente], [LugarAccidente], [TrabajoHabitual], [RealizabaHabitual], [ClasificacionAccidente], [TipoAccidente], [TipoTrayecto], [MedioPrueba], [DetallePrueba] FROM [DenunciasCasos] WHERE [Formulario]='DIAT' EXCEPT SELECT [Id], [CasoId], [HoraAccidente], [HoraIngreso], [HoraSalida], [AccidenteDireccion], [AccidenteComuna], [ActividadAccidente], [LugarAccidente], [TrabajoHabitual], [RealizabaHabitual], [ClasificacionAccidente], [TipoAccidente], [TipoTrayecto], [MedioPrueba], [DetallePrueba] FROM [DIAT]) THROW 51002,'No coinciden los datos de DIAT.',1;
INSERT INTO [DIEP] ([Id], [CasoId], [DuracionSintomas], [DuracionSintomasUnidad], [MolestiasPrevias], [ParteCuerpo], [ActividadSintomas], [PuestoSintomas], [CompanerosSintomas], [Agentes], [TiempoExposicion], [TiempoExposicionUnidad]) SELECT [Id], [CasoId], [DuracionSintomas], [DuracionSintomasUnidad], [MolestiasPrevias], [ParteCuerpo], [ActividadSintomas], [PuestoSintomas], [CompanerosSintomas], [Agentes], [TiempoExposicion], [TiempoExposicionUnidad] FROM [DenunciasCasos] WHERE [Formulario]='DIEP';
IF (SELECT COUNT(*) FROM [DIEP])<>(SELECT COUNT(*) FROM [DenunciasCasos] WHERE [Formulario]='DIEP') THROW 51001,'No coinciden los registros de DIEP.',1;
IF EXISTS (SELECT [Id], [CasoId], [DuracionSintomas], [DuracionSintomasUnidad], [MolestiasPrevias], [ParteCuerpo], [ActividadSintomas], [PuestoSintomas], [CompanerosSintomas], [Agentes], [TiempoExposicion], [TiempoExposicionUnidad] FROM [DenunciasCasos] WHERE [Formulario]='DIEP' EXCEPT SELECT [Id], [CasoId], [DuracionSintomas], [DuracionSintomasUnidad], [MolestiasPrevias], [ParteCuerpo], [ActividadSintomas], [PuestoSintomas], [CompanerosSintomas], [Agentes], [TiempoExposicion], [TiempoExposicionUnidad] FROM [DIEP]) THROW 51002,'No coinciden los datos de DIEP.',1;
UPDATE c SET [DenunciaJson]=NULL FROM [Casos] c JOIN [DenunciasCasos] d ON d.[CasoId]=c.[Id];

""");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
}
