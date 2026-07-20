#region Usings

using SupportPulse.Core.DTOs.Common;

#endregion

namespace SupportPulse.Core.Services.Common
{
    /// <summary>
    /// Provides factory methods for creating <see cref="OperationResult"/> and <see cref="OperationResult{T}"/> instances.
    /// </summary>
    public interface IOperationResultAction
    {
        /// <summary>
        /// Creates an error <see cref="OperationResult"/> with the specified message and status.
        /// </summary>
        /// <param name="message">The error message (displayed to the user).</param>
        /// <param name="status">The operation status (defaults to <see cref="OperationStatus.Error"/>).</param>
        OperationResult SendError(
            string? message = "هنگام انجام عملیات خطایی رخ داد.",
            OperationStatus? status = OperationStatus.Error);

        /// <summary>
        /// Creates an error <see cref="OperationResult{T}"/> with the specified message, status, and optional entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="message">The error message (displayed to the user).</param>
        /// <param name="status">The operation status (defaults to <see cref="OperationStatus.Error"/>).</param>
        /// <param name="entity">Optional entity to include.</param>
        OperationResult<T> SendError<T>(
            string? message = "هنگام انجام عملیات خطایی رخ داد.",
            OperationStatus? status = OperationStatus.Error,
            T? entity = default);

        /// <summary>
        /// Creates a success <see cref="OperationResult"/> with the specified message and status.
        /// </summary>
        /// <param name="message">The success message (displayed to the user).</param>
        /// <param name="status">The operation status (defaults to <see cref="OperationStatus.Success"/>).</param>
        OperationResult SendSuccess(
            string? message = "عملیات با موفقیت انجام شد.",
            OperationStatus? status = OperationStatus.Success);

        /// <summary>
        /// Creates a success <see cref="OperationResult{T}"/> with the specified message, status, and entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="message">The success message (displayed to the user).</param>
        /// <param name="status">The operation status (defaults to <see cref="OperationStatus.Success"/>).</param>
        /// <param name="entity">The entity to include in the result.</param>
        OperationResult<T> SendSuccess<T>(
            string? message = "عملیات با موفقیت انجام شد.",
            OperationStatus? status = OperationStatus.Success,
            T? entity = default);
    }
}