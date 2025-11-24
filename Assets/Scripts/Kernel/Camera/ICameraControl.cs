using UnityEngine;

namespace Kernel
{
    public abstract class CameraControl: MonoBehaviour
    {
        public float panSpeed = 20f;
        public float zoomSpeed = 2f;
        public float minZoom = 5f;
        public float maxZoom = 50f;
        public float edgeThickness = 20f;
        public float edgePanSpeedMultiplier = 0.85f;
        public float mouseDragPanSpeed = 0.85f;
        public bool enableEdgePan = true;
        public abstract Camera CameraComponent { get; set; }

        /// <summary>
        /// 处理摄像机平移：支持键盘、鼠标中键拖动、屏幕边缘移动
        /// </summary>
        /// <param name="无">无</param>
        /// <returns>无</returns>
        protected virtual void HandlePan()
        {
            // 1. 计算当前平移速度（修掉原来 panSpeed 每帧被乘的 bug）
            float currentPanSpeed = panSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentPanSpeed *= 1.8f;
            }

            // ============================================================
            // 2. 键盘输入（WASD / 方向键）
            // ============================================================
            float keyX = Input.GetAxisRaw("Horizontal"); // A/D 或 左/右
            float keyY = Input.GetAxisRaw("Vertical");   // W/S 或 上/下

            Vector3 keyMove = new Vector3(keyX, keyY, 0f);

            // ============================================================
            // 3. 屏幕边缘移动
            // ============================================================
            Vector3 edgeMove = Vector3.zero;
            Vector3 mousePos = Input.mousePosition;

            // 左右边缘
            if (mousePos.x <= edgeThickness)
            {
                edgeMove.x = -1f;
            }
            else if (mousePos.x >= Screen.width - edgeThickness)
            {
                edgeMove.x = 1f;
            }

            // 上下边缘
            if (mousePos.y <= edgeThickness)
            {
                edgeMove.y = -1f;
            }
            else if (mousePos.y >= Screen.height - edgeThickness)
            {
                edgeMove.y = 1f;
            }

            // 边缘移动可以加一个单独的系数
            if (edgeMove != Vector3.zero)
            {
                edgeMove *= edgePanSpeedMultiplier;
            }
            if(!enableEdgePan)
            {
                edgeMove = Vector3.zero;
            }
            // ============================================================
            // 4. 合并键盘 + 边缘移动
            //    为了避免对角线更快，做一个简单归一
            // ============================================================
            Vector3 panDir = keyMove + edgeMove;
            if (panDir.sqrMagnitude > 1f)
            {
                panDir.Normalize();
            }

            Vector3 panMove = panDir * currentPanSpeed * Time.deltaTime;

            // ============================================================
            // 5. 鼠标中键拖动
            //    按住鼠标中键时，根据鼠标移动量反向平移相机
            // ============================================================
            Vector3 dragMove = Vector3.zero;

            if (Input.GetMouseButton(2)) // 2 = 中键
            {
                // Mouse X/Y 是屏幕上的相对移动，这里取反让拖动方向更自然（拖到右边 = 视角往右）
                float dragX = -Input.GetAxis("Mouse X");
                float dragY = -Input.GetAxis("Mouse Y");

                dragMove = new Vector3(dragX, dragY, 0f) * mouseDragPanSpeed;
                // 一般这里不乘 Time.deltaTime，感觉更跟手；
                // 如果你觉得太快，可以改成 * mouseDragPanSpeed * Time.deltaTime
            }

            // ============================================================
            // 6. 应用最终位移
            // ============================================================
            transform.Translate(panMove + dragMove, Space.World);
        }

        protected virtual void HandleZoom()
        {
            panSpeed = Mathf.Lerp(10f, 50f, (CameraComponent.orthographicSize - minZoom) / (maxZoom - minZoom));
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                float newSize = CameraComponent.orthographicSize - scroll * zoomSpeed;
                CameraComponent.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            }
        }
    }


}