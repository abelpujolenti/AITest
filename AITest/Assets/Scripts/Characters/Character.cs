using System.Collections;
using System.Diagnostics;
using Managers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Characters
{
    public class Character : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;
        
        [Min(1)]
        [SerializeField] private float _detectionRadius;

        [Min(1)]
        [SerializeField] private float _speed;
        
        [Min(1)]
        [SerializeField] private float _jumpForce;

        private Coroutine _moveXZAxisCoroutine;
        private Coroutine _releaseJumpCoroutine;
        private Coroutine _pressMouseButton0Coroutine;
        private Coroutine _pressMouseButton1Coroutine;
        private Coroutine _pressMouseButton2Coroutine;

        private Stopwatch _jumpStopWatch = new Stopwatch();

        private float _axisXMove;
        private float _axisYMove;
        private float _axisZMove;
        
        private Vector3 _finalMoveVector;

        private bool _isMovingOnXAxis;
        private bool _isMovingOnZAxis;
        private bool _isMoving;

        private bool _canJump = true;
        private bool _pressingJump;
        
        void Start()
        {
            EventsManager.MoveForward += MoveForward;
            EventsManager.MoveLeft += MoveLeft;
            EventsManager.MoveBackwards += MoveBackwards;
            EventsManager.MoveRight += MoveRight;
            EventsManager.PressJump += PressJump;
            
            EventsManager.PressMouseButton0 += PressMouseButton0;
            EventsManager.PressMouseButton1 += PressMouseButton1;
            EventsManager.PressMouseButton2 += PressMouseButton2;
            
            EventsManager.ScrollUp += ScrollUp;
            EventsManager.ScrollDown += ScrollDown;
        }

        private void MoveForward()
        {
            EventsManager.MoveForward -= MoveForward;
            EventsManager.StopMovingForward += StopMovingForward;
            _isMovingOnZAxis ^= true;
            _axisZMove += 1;
            CalculateFinalMoveVector();
        }

        private void StopMovingForward()
        {
            EventsManager.StopMovingForward -= StopMovingForward;
            EventsManager.MoveForward += MoveForward;
            _axisZMove -= 1;
            _isMovingOnZAxis = _axisZMove != 0;
            CalculateFinalMoveVector();
        }

        private void MoveBackwards()
        {
            EventsManager.MoveBackwards -= MoveBackwards;
            EventsManager.StopMovingBackwards += StopMovingBackwards;
            _isMovingOnZAxis ^= true;
            _axisZMove += -1;
            CalculateFinalMoveVector();
        }

        private void StopMovingBackwards()
        {
            EventsManager.StopMovingBackwards -= StopMovingBackwards;
            EventsManager.MoveBackwards += MoveBackwards;
            _axisZMove -= -1;
            _isMovingOnZAxis = _axisZMove != 0;
            CalculateFinalMoveVector();
        }

        private void MoveLeft()
        {
            EventsManager.MoveLeft -= MoveLeft;
            EventsManager.StopMovingLeft += StopMovingLeft;
            _isMovingOnXAxis ^= true;
            _axisXMove += -1;
            CalculateFinalMoveVector();
        }

        private void StopMovingLeft()
        {
            EventsManager.StopMovingLeft -= StopMovingLeft;
            EventsManager.MoveLeft += MoveLeft;
            _axisXMove -= -1;
            _isMovingOnXAxis = _axisXMove != 0;
            CalculateFinalMoveVector();
        }

        private void MoveRight()
        {
            EventsManager.MoveRight -= MoveRight;
            EventsManager.StopMovingRight += StopMovingRight;
            _isMovingOnXAxis ^= true;
            _axisXMove += 1;
            CalculateFinalMoveVector();
        }

        private void StopMovingRight()
        {
            EventsManager.StopMovingRight -= StopMovingRight;
            EventsManager.MoveRight += MoveRight;
            _axisXMove -= 1;
            _isMovingOnXAxis = _axisXMove != 0;
            CalculateFinalMoveVector();
        }

        private void PressJump()
        {
            if (!_canJump)
            {
                return;
            }

            _canJump = false;
            _pressingJump = true;
            EventsManager.PressJump -= PressJump;
            EventsManager.ReleaseJump += ReleaseJump;
            _axisYMove = _jumpForce;
            CalculateFinalMoveVector();
            _releaseJumpCoroutine = StartCoroutine(PressingJumpCoroutine());
        }

        private IEnumerator PressingJumpCoroutine()
        {
            float maxJumpingTime = _jumpForce / -Physics.gravity.y;
            float currentTime = 0;
            float gravityDivider = 4;
            
            while (_pressingJump && _rigidbody.velocity.y > 0)
            {
                _axisYMove += (Physics.gravity.y / gravityDivider) * Time.deltaTime;
                gravityDivider = Mathf.Lerp(4, 1, Mathf.Pow(currentTime / maxJumpingTime, 2));
                currentTime += Time.deltaTime;
                CalculateFinalMoveVector();
                yield return null;
            }

            while (!_canJump)
            {
                _axisYMove += Physics.gravity.y * Time.deltaTime;
                CalculateFinalMoveVector();
                yield return null;
            }

            _axisYMove = 0;
            CalculateFinalMoveVector();
            _releaseJumpCoroutine = null;
        }

        private void ReleaseJump()
        {
            EventsManager.ReleaseJump -= ReleaseJump;
            EventsManager.PressJump += PressJump;
            _pressingJump = false;
        }

        private void CalculateFinalMoveVector()
        {
            _isMoving = _isMovingOnXAxis || _isMovingOnZAxis;

            if (!_isMoving)
            {
                _rigidbody.velocity = new Vector3(0, _axisYMove, 0);
                return;
            }

            _finalMoveVector = new Vector3(_axisXMove, 0, _axisZMove).normalized * _speed + 
                               new Vector3(0, _axisYMove, 0);
            
            if (_moveXZAxisCoroutine != null)
            {
                return;
            }
            _moveXZAxisCoroutine = StartCoroutine(MoveXZAxisCoroutine());
        }

        private IEnumerator MoveXZAxisCoroutine()
        {
            while (_isMoving)
            {
                _rigidbody.velocity = _finalMoveVector;
                yield return null;
            }
            
            _moveXZAxisCoroutine = null;
        }

        private void PressMouseButton0()
        {
            EventsManager.PressMouseButton0 -= PressMouseButton0; 
            EventsManager.ReleaseMouseButton0 += ReleaseMouseButton0;
            //Debug.Log("Press Mouse Button 0");
            _pressMouseButton0Coroutine = StartCoroutine(PressMouseButton0Coroutine());
        }

        private IEnumerator PressMouseButton0Coroutine()
        {
            while (true)
            {
                //Debug.Log("Pressing Mouse Button 0");
                yield return null;
            }
        }

        private void ReleaseMouseButton0()
        {
            EventsManager.ReleaseMouseButton0 -= ReleaseMouseButton0; 
            EventsManager.PressMouseButton0 += PressMouseButton0; 
            StopCoroutine(_pressMouseButton0Coroutine);
            //Debug.Log("Release Mouse Button 0");
        }

        private void PressMouseButton1()
        {
            EventsManager.PressMouseButton1 -= PressMouseButton1;
            EventsManager.ReleaseMouseButton1 += ReleaseMouseButton1;
            //Debug.Log("Press Mouse Button 1");
            _pressMouseButton1Coroutine = StartCoroutine(PressMouseButton1Coroutine());
        }

        private IEnumerator PressMouseButton1Coroutine()
        {
            while (true)
            {
                //Debug.Log("Pressing Mouse Button 1");
                yield return null;
            }
        }

        private void ReleaseMouseButton1()
        {
            EventsManager.ReleaseMouseButton1 -= ReleaseMouseButton1; 
            EventsManager.PressMouseButton1 += PressMouseButton1; 
            StopCoroutine(_pressMouseButton1Coroutine);
            //Debug.Log("Release Mouse Button 1");
        }

        private void PressMouseButton2()
        {
            EventsManager.PressMouseButton2 -= PressMouseButton2;
            EventsManager.ReleaseMouseButton2 += ReleaseMouseButton2;
            //Debug.Log("Press Mouse Button 2");
            _pressMouseButton2Coroutine = StartCoroutine(PressMouseButton2Coroutine());
        }

        private IEnumerator PressMouseButton2Coroutine()
        {
            while (true)
            {
                //Debug.Log("Pressing Mouse Button 2");
                yield return null;
            }
        }

        private void ReleaseMouseButton2()
        {
            EventsManager.ReleaseMouseButton2 -= ReleaseMouseButton2; 
            EventsManager.PressMouseButton2 += PressMouseButton2; 
            StopCoroutine(_pressMouseButton2Coroutine);
            //Debug.Log("Release Mouse Button 2");
        }

        private void ScrollUp()
        {
            Debug.Log("Scroll Up");
        }


        private void ScrollDown()
        {
            Debug.Log("Scroll Down");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != GameManager.Instance.GetEnemyLayer())
            {
                return;
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.layer != GameManager.Instance.GetTerrainLayer())
            {
                return;
            }

            if (Vector3.Angle(other.GetContact(0).normal, Vector3.up) <= 60f)
            {
                _canJump = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            //throw new NotImplementedException();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(gameObject.transform.position, _detectionRadius);
        }
    }
}