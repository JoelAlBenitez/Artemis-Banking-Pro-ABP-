# Beneficiarios — DTOs y ViewModels

## `SaveBeneficiaryDto` (escritura)

| Campo | Tipo | Nota |
|---|---|---|
| `OwnerClientId` | string | Cliente autenticado (se asigna desde `ICurrentUserService`, no desde el formulario) |
| `AccountNumber` | string(9) | Número de cuenta a registrar como beneficiaria |

## `BeneficiaryDto` (lectura)

| Campo | Tipo | Nota |
|---|---|---|
| `Id` | int | Identificador de la relación |
| `AccountNumber` | string(9) | Cuenta beneficiaria |
| `OwnerFullName` | string | Nombre y apellido del propietario de la cuenta beneficiaria (resuelto desde Identity) |

## `SaveBeneficiaryViewModel`

```csharp
public class SaveBeneficiaryViewModel : BaseViewModel
{
    [Required(ErrorMessage = "El número de cuenta es requerido.")]
    [StringLength(9, MinimumLength = 9,
        ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
    public string AccountNumber { get; set; } = string.Empty;
}
```

## `BeneficiaryListViewModel`

| Campo | Descripción |
|---|---|
| `Name` | Nombre del propietario de la cuenta beneficiaria |
| `LastName` | Apellido del propietario |
| `AccountNumber` | Número de cuenta (9 dígitos) |

> El nombre y apellido se resuelven vía Identity al momento de listar; no se persisten en
> `Beneficiary`.

> **UI / vistas: No desarrollado por el agente de IA.**
