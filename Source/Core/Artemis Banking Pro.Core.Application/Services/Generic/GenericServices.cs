using Artemis_Banking_Pro.Core.Application.Contracts.Generic;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Generic
{

   public abstract class GenericServices<TSaveDto, TDto, Tkey, TEntity>
       : IGenericServices<TSaveDto, TDto, Tkey>
       where TEntity : BaseEntitie<Tkey>
       where TSaveDto : class
       where TDto : class
   {
        protected readonly IGenericRepository<TEntity, Tkey> _genericRepository;
        protected readonly IMapper _mapper;
        private readonly ILogger<GenericServices<TSaveDto, TDto, Tkey, TEntity>> _logger; 

        public GenericServices(IGenericRepository<TEntity, Tkey> genericRepository,
            IMapper mapper,
            ILogger<GenericServices<TSaveDto, TDto, Tkey, TEntity>> logger
            )
        {
            _genericRepository = genericRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public virtual async Task<ValidationResult> CreateAsync(TSaveDto dto)
        {
            try
            {
                _logger.LogInformation("Mapeando el dto {dto} a la entidad {entity}",
                typeof(TSaveDto).Name, typeof(TEntity).Name);
                var map = _mapper.Map<TEntity>(dto);
                _logger.LogInformation("Intento de guardar el registro de la entidad {entity}", typeof(TEntity).Name);
                await _genericRepository.AddAsync(map);
                await _genericRepository.SaveChangesAsync();
                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el registro  de la entidad {entity}", typeof(TEntity).Name);
                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public virtual async Task<ValidationResult<PagedResult<TDto>>> GetAllAsync(int page, int pageSize)
        {
            try
            {
                _logger.LogInformation("Recuperando los datos de la entidad {entity}", typeof(TEntity).Name);
                var result = await _genericRepository.GetAllAsync(page, pageSize);

                _logger.LogInformation("Mapeando la entidad {entity} al dto {dto}",
                typeof(TEntity).Name, typeof(TDto).Name);
                var map = _mapper.Map<IReadOnlyCollection<TDto>>(result.Items);

                _logger.LogInformation("Paginación de los datos recuperados de la entidad {entity}", typeof(TEntity).Name);
                var paged = new PagedResult<TDto>(map, result.Page, result.PageSize, result.TotalRecords);
                return ValidationResult<PagedResult<TDto>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener  los registros  de la entidad {entity}", typeof(TEntity).Name);
                return ValidationResult<PagedResult<TDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public virtual async Task<ValidationResult<TDto>> GetByIdAsync(Tkey tkey)
        {
            try
            {
                var entitie = await _genericRepository.GetByIdAsync(tkey);
                if(entitie is null)
                {
                    return ValidationResult<TDto>.Failure(GeneralError.NonExistence);
                }
                var map = _mapper.Map<TDto>(entitie);
                return ValidationResult<TDto>.Success(map);

            } catch (Exception)
            {
                return ValidationResult<TDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public virtual async Task<ValidationResult> UpdateAsync(Tkey tkey, TSaveDto dto)
        {
            try
            {
                var entitie = await _genericRepository.GetByIdAsync(tkey);
                if (entitie is null)
                {
                    return ValidationResult.Failure(GeneralError.NonExistence);
                }

                _mapper.Map(dto, entitie);
                await _genericRepository.UpdateAsync(entitie);
                await _genericRepository.SaveChangesAsync();

                return ValidationResult.Success();
            }
            catch (Exception)
            {
                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }
    }

   }
