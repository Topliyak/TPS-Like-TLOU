using UnityEngine;

public class FewModesButton
{
	public KeyCode Button { get; private set; }

	private bool _ShouldCheck = false;
	private float[] _Delays;
	private bool[] _ModesStatuses;
	private float _PressedTime = 0;
	private float _PressedMoment = 0;

	public FewModesButton(KeyCode button, params float[] delays)
	{
		Button = button;

		_Delays = new float[delays.Length];
		for (int i = 0; i < _Delays.Length; i++) _Delays[i] = delays[i];

		_ModesStatuses = new bool[delays.Length + 1];
		for (int i = 0; i < _ModesStatuses.Length; i++) _ModesStatuses[i] = false;
	}

	public void Update()
	{
		if (!_ShouldCheck)
			for (int i = 0; i < _ModesStatuses.Length; i++) _ModesStatuses[i] = false;

		if (Input.GetKeyDown(Button))
		{
			_ShouldCheck = true;
			_PressedMoment = Time.realtimeSinceStartup;

		}

		if (_ShouldCheck && Input.GetKey(Button))
		{
			_PressedTime = Time.realtimeSinceStartup - _PressedMoment;
			
			if (_PressedTime >= _Delays[_Delays.Length - 1])
			{
				_ShouldCheck = false;
				_ModesStatuses[_ModesStatuses.Length - 1] = true;
			}
		}

		if (_ShouldCheck && Input.GetKeyUp(Button))
		{
			_ShouldCheck = false;
			_PressedTime = Time.realtimeSinceStartup - _PressedMoment;

			for (int i = 0; i < _ModesStatuses.Length; i++)
				_ModesStatuses[i] = (i == 0 || _PressedTime >= _Delays[i - 1]) && 
									(i == _ModesStatuses.Length - 1 || _PressedTime < _Delays[i]);
		}
	}

	public bool GetModeStatus(int index) => _ModesStatuses[index];
}
