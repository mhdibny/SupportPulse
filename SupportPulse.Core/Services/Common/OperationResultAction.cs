#region Usings

using SupportPulse.Core.DTOs.Common;

#endregion

namespace SupportPulse.Core.Services.Common
{
    /// <summary>
    /// Default implementation of <see cref="IOperationResultAction"/> that simply instantiates result objects.
    /// </summary>
    public class OperationResultAction : IOperationResultAction
    {
        #region Error Results

        /// <inheritdoc />
        public OperationResult SendError(
            string? message = "هنگام انجام عملیات خطایی رخ داد.",
            OperationStatus? status = OperationStatus.Error)
        {
            return new OperationResult
            {
                IsSuccess = false,
                Message = message,
                Status = status ?? OperationStatus.Error
            };
        }

        /// <inheritdoc />
        public OperationResult<T> SendError<T>(
            string? message = "هنگام انجام عملیات خطایی رخ داد.",
            OperationStatus? status = OperationStatus.Error,
            T? entity = default)
        {
            return new OperationResult<T>
            {
                Data = entity,
                IsSuccess = false,
                Message = message,
                Status = status ?? OperationStatus.Error
            };
        }

        #endregion

        #region Success Results

        /// <inheritdoc />
        public OperationResult SendSuccess(
            string? message = "عملیات با موفقیت انجام شد.",
            OperationStatus? status = OperationStatus.Success)
        {
            return new OperationResult
            {
                IsSuccess = true,
                Message = message,
                Status = status ?? OperationStatus.Success
            };
        }

        /// <inheritdoc />
        public OperationResult<T> SendSuccess<T>(
            string? message = "عملیات با موفقیت انجام شد.",
            OperationStatus? status = OperationStatus.Success,
            T? entity = default)
        {
            return new OperationResult<T>
            {
                Data = entity,
                IsSuccess = true,
                Message = message,
                Status = status ?? OperationStatus.Success
            };
        }

        #endregion
    }
}