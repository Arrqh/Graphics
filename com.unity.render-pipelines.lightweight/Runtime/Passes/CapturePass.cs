using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Let customizable actions inject commands to capture the camera output.
    ///
    /// You can use this pass to inject capture commands into a command buffer
    /// with the goal of having camera capture happening in external code.
    /// </summary>
    internal class CapturePass : ScriptableRenderPass
    {
        RenderTargetHandle m_CameraColorHandle;

        public CapturePass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="actions"></param>
        public void Setup(RenderTargetHandle colorHandle)
        {
            m_CameraColorHandle = colorHandle;
            profilerTag = "Capture Pass";
        }

        public override bool ShouldExecute(ref RenderingData renderingData)
        {
            return renderingData.cameraData.captureActions != null;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmdBuf = CommandBufferPool.Get(profilerTag);
            var colorAttachmentIdentifier = m_CameraColorHandle.Identifier();
            var captureActions = renderingData.cameraData.captureActions;
            for (captureActions.Reset(); captureActions.MoveNext();)
                captureActions.Current(colorAttachmentIdentifier, cmdBuf);

            context.ExecuteCommandBuffer(cmdBuf);
            CommandBufferPool.Release(cmdBuf);
        }
    }
}
