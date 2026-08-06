namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    //Resultado de la evaluación de riesgo previa a registrar un préstamo (pág. 38 del documento
    //funcional). La Web API lo traduce a la propiedad riskType de su respuesta 409 Conflict.
    public enum LoanRiskType
    {
        //Deuda proyectada menor o igual al promedio: el préstamo se crea sin advertencia
        SinRiesgo = 1,

        //La deuda actual del cliente ya supera el promedio del sistema
        DeudaActual = 2,

        //La deuda actual no supera el promedio, pero la proyectada con el nuevo préstamo sí
        DeudaProyectada = 3
    }
}
