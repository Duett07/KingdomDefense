using NUnit.Framework;
using UnityEngine;
using KingdomDefense.Core;

namespace KingdomDefense.Tests
{
    public class GameManagerTests
    {
        private GameObject _gameManagerObj;
        private GameManager _gameManager;

        [SetUp]
        public void SetUp()
        {
            _gameManagerObj = new GameObject("Test_GameManager");
            _gameManager = _gameManagerObj.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameManagerObj != null)
            {
                Object.DestroyImmediate(_gameManagerObj);
            }
        }

        [Test]
        public void CalculateStars_WithTwentyLives_ReturnsThreeStars()
        {
            int stars = _gameManager.CalculateStars(20);
            Assert.AreEqual(3, stars, "20 Máu bảo toàn nguyên vẹn phải đạt 3 Sao.");
        }

        [Test]
        public void CalculateStars_WithTenToNineteenLives_ReturnsTwoStars()
        {
            Assert.AreEqual(2, _gameManager.CalculateStars(19), "19 Máu phải đạt 2 Sao.");
            Assert.AreEqual(2, _gameManager.CalculateStars(15), "15 Máu phải đạt 2 Sao.");
            Assert.AreEqual(2, _gameManager.CalculateStars(10), "10 Máu phải đạt 2 Sao.");
        }

        [Test]
        public void CalculateStars_WithUnderTenLives_ReturnsOneStar()
        {
            Assert.AreEqual(1, _gameManager.CalculateStars(9), "9 Máu phải đạt 1 Sao.");
            Assert.AreEqual(1, _gameManager.CalculateStars(5), "5 Máu phải đạt 1 Sao.");
            Assert.AreEqual(1, _gameManager.CalculateStars(1), "1 Máu phải đạt 1 Sao.");
        }

        [Test]
        public void InitialState_IsPlaying()
        {
            Assert.AreEqual(GameState.Playing, _gameManager.CurrentState, "Trạng thái khởi đầu phải là Playing.");
            Assert.IsFalse(_gameManager.IsGameOver, "Ban đầu không phải là Game Over.");
        }
    }
}
