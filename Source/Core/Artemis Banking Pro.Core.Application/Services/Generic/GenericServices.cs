using Artemis_Banking_Pro.Core.Application.Contracts.Generic;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using AutoMapper;


namespace Artemis_Banking_Pro.Core.Application.Services.Generic
{
   public abstract class GenericServices<TDto, Tkey, TEntity>
       : IGenericServices<TDto, Tkey>
       where TEntity : BaseEntitie<Tkey>
       where TDto : class
   {
        protected readonly IGenericRepository<TEntity, Tkey> _genericRepository;
        protected readonly IMapper _mapper;
        protected readonly List<Error> _errors = new List<Error>();

        public GenericServices(IGenericRepository<TEntity, Tkey> genericRepository, 
            IMapper mapper,
            List<Error> errors)
        {
            _genericRepository = genericRepository;
            _mapper = mapper;
            _errors = errors;
        }

        public virtual async Task<ValidationResult> CreateAsync(TDto dto)
        {
            try
            {
                var map = _mapper.Map<TEntity>(dto);
                await _genericRepository.AddAsync(map);
                return ValidationResult.Failure();
            }
            catch (Exception)
            {
                _errors.Add(GeneralError.UnexpectedError);
                return ValidationResult.Failure(_errors);
            }
        }

        public virtual async Task<ValidationResult<IReadOnlyCollection<TDto>>> GetAllAsync(int page, int pageSize)
        {
            try
            {
                var result = await _genericRepository.GetAllAsync(page, pageSize);
                var map = _mapper.Map <IReadOnlyCollection<TDto>>(result);
                return ValidationResult<IReadOnlyCollection<TDto>>.Success(map);
            }
            catch (Exception)
            {
                _errors.Add(GeneralError.UnexpectedError);
                return ValidationResult<IReadOnlyCollection<TDto>>.Failure(_errors);
            }
        }

        public virtual async Task<ValidationResult<TDto>> GetByIdAsync(Tkey tkey)
        {
            try
            {
                var entite = await _genericRepository.GetByIdAsync(tkey);
                if(entite is null)
                {
                    _errors.Add(GeneralError.NonExistence);
                    return ValidationResult<TDto>.Failure(_errors);
                }
                var map = _mapper.Map<TDto>(entite);
                return ValidationResult<TDto>.Success(map);

            } catch (Exception)
            {
                _errors.Add(GeneralError.UnexpectedError);
                return ValidationResult<TDto>.Failure(_errors);
            }
        }

        public virtual async Task<ValidationResult> UpdateAsync(TDto dto)
        {
            try
            {
               var map = _mapper.Map<TEntity>(dto);
               var  success =  await _genericRepository.UpdateAsync(map);
                if (!success)
                {
                    _errors.Add(GeneralError.UnexpectedError);
                    return ValidationResult<IReadOnlyCollection<TDto>>.Failure(_errors);

                }
                return ValidationResult.Success();
            }
            catch (Exception)
            {
                _errors.Add(GeneralError.UnexpectedError);
                return ValidationResult<IReadOnlyCollection<TDto>>.Failure(_errors);
            }
        }
    }

   }

