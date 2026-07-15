
[00:00:08.539,703] <wrn> lte_mgr: LTE 기본 진단 시작: registered
[00:00:08.540,588] <inf> lte_mgr: LTE 모드[registered]: 시스템=ltem(1) 우선=auto(0)
[00:00:08.541,564] <inf> lte_mgr: LTE 모드[registered]: LTE=ltem(7)
[00:00:08.542,358] <inf> lte_mgr: LTE 모드[registered]: 기능=normal(1)
[00:00:08.543,304] <inf> lte_mgr: LTE 기본 진단 시작: +COPS: 0,2,"45006",7  OK
[00:00:08.544,494] <inf> lte_mgr: LTE 기본 진단 시작: +CEREG: 5,5,"420F","00E2BD12",7,,,"00001010","00110100"  OK
[00:00:08.545,837] <inf> lte_mgr: LTE 기본 진단 시작: %XSYSTEMMODE: 1,0,0,0  OK
[00:00:08.546,661] <inf> lte_mgr: LTE 기본 진단 시작: +CESQ: 99,99,255,255,8,61  OK
[00:00:08.547,790] <inf> lte_mgr: LTE 기본 진단 시작: %XMONITOR: 5,"","","45006","420F",7,5,"00E2BD12",27,2600,61,24,"","00001010","00110100","01001001"  OK
[00:00:08.548,919] <inf> lte_mgr: 현재 RSRP: 61: -80 dBm
[00:00:08.549,316] <inf> lte_mgr: 현재 RSRQ: 8: -16.0 dB
[00:00:08.550,201] <inf> lte_mgr: 현재 SNR: 24: 0 dB
[00:00:08.550,567] <wrn> lte_mgr: LTE 기본 진단 종료
[00:00:08.860,382] <inf> lte_mgr: >> 위치 감지: 한국 (MCC 450)
[00:00:08.860,778] <inf> lte_mgr: >> PSM을 위해 LGU+ (45006)로 전환...
[00:00:08.862,548] <inf> lte_mgr: LTE 등록됨(망 등록 상태), PDN 확인 진행
[00:00:08.863,494] <inf> main: 📥 서버 스케줄 가져오는 중...
[00:00:08.863,891] <inf> net_http: ==================net_http_get_command==================
[00:00:09.346,221] <inf> net_http: HTTP GET: connecting to www.feedcheck.kr:80...
[00:00:09.756,317] <inf> net_http: ✅ HTTP GET: 연결 완료
[00:00:09.756,774] <inf> net_http: HTTP GET 요청 전송: /view/feedcheckConnectToServer?mac=78-57-22-31-00-82
[00:00:09.757,659] <inf> net_http: ✅ HTTP GET 요청 전송됨: 115 바이트
[00:00:09.758,087] <inf> net_http: HTTP GET: 응답 대기 중...
[00:00:10.256,896] <inf> net_http: 🔌 연결 종료 ACK 전송 중...
[00:00:10.257,873] <inf> net_http: 서버 응답 수신: 647 bytes
[00:00:10.258,270] <inf> net_http: ==================net_http_parse_server_response==================
[00:00:10.258,911] <inf> net_http: 서버 시간 (stm): 1769414598
[00:00:10.587,860] <wrn> flash_manager: ⚠️ NVS full (-28). Clearing old error logs and retrying...
[00:00:10.714,355] <inf> flash_manager: ✅ Recovered from NVS full.
[00:00:10.714,782] <inf> flash_manager: ✅ Error log saved to flash: device=🚀서버 전송완료 OKKKK, 서버시간 수신완료: 1769414598, error_code=0, timestamp=1769414598 (count: 20/20)
[00:00:10.829,528] <inf> flash_manager: ✅ Server command saved to flash:
[00:00:10.829,956] <inf> flash_manager:   lidar_times_count=48
[00:00:10.830,322] <inf> flash_manager:   temp_times_count=2 [주기=300, 전송방법=0]
[00:00:10.830,810] <inf> flash_manager:   time_gap_count=8
[00:00:10.831,176] <inf> net_http: ✅ 서버 응답 파싱 및 저장 완료. 스케줄: 48 개
[00:00:10.831,665] <inf> net_http: ✅ 서버 명령 수신 완료
[00:00:10.832,031] <inf> main: ✅ 서버 스케줄 업데이트 완료!
[00:00:10.832,489] <inf> lidar_pwr: ✅ 라이다 전원 GPIO 초기화 완료 (pin=0, port=gpio@842500)
[00:00:10.884,704] <inf> lidar_spi: ✅ SPI Double Buffering 초기화 완료 (A/B)
[00:00:10.885,162] <inf> battery_v_check: ✅ Battery ADC initialized (AIN3/P0.16)
[00:00:10.890,716] <inf> lidar_pwr: ✅ 라이다 전원 OFF
[00:00:10.891,082] <inf> loadswitch: ✅ 로드 스위치 OFF
[00:00:15.901,672] <inf> main: ⏰ 스케줄 확인: 현재시간=170323, 다음스케줄=171500
[00:00:36.486,602] <inf> tact_switch: 🔘 Tact Switch SHORT PRESS (<1s) (161 ms)
[00:00:36.487,091] <inf> main: ===========>📊 버튼이벤트 lidar sequence start
[00:00:36.487,548] <inf> control_tower: ========================================================
[00:00:36.488,006] <inf> control_tower: ============= 라이다 측정 시퀀스 시작 =========
[00:00:36.488,494] <inf> control_tower: 펌웨어 버전: 1.0.0+0
[00:00:36.488,861] <inf> control_tower: ========================================================
[00:00:36.589,630] <inf> i2c_vcc_en: ✅ I2C VCC EN initialized (P0.23, Active Low)
[00:00:36.590,057] <inf> i2c_vcc_en: ✅ I2C VCC EN: ON (P0.23 = Low)
[00:00:36.690,521] <err> i2c_gyro_sensor: MC3419 device not ready
[00:00:36.690,917] <err> i2c_temp_sensor: AHT20 device not ready
[00:00:36.691,314] <inf> i2c_vcc_en: ✅ I2C VCC EN: OFF (P0.23 = High)
[00:00:36.691,741] <inf> control_tower: 1단계: 로드 스위치 ON
[00:00:36.692,108] <inf> loadswitch: ✅ 로드 스위치 ON

[00:00:36.994,934] <inf> flash_manager: ✅ BATT log loaded from flash: timestamp=1769413831, cnt=166
[00:00:36.995,544] <inf> control_tower: 2단계: 배터리 전압 10.0V 이상 대기 중...
[00:00:37.298,980] <inf> control_tower: 3단계: 라이다 전원 ON
[00:00:37.313,262] <inf> lidar_spi: ✅ SPI 트랜잭션 복구 ���료
[00:00:37.323,730] <inf> lidar_pwr: ✅ 라이다 전원 ON
[00:00:37.824,157] <inf> control_tower: ⏳ SPI 데이터 수신 대기 중...
[00:00:40.852,569] <inf> control_tower: 3단계: 라이다 전원 OFF
[00:00:40.858,062] <inf> lidar_pwr: ✅ 라이다 전원 OFF
[00:00:40.863,037] <inf> flash_manager: ✅ BATT log saved to flash: timestamp=1769414624, cnt=167, v_ok_idx=3
[00:00:40.863,616] <inf> control_tower: ✅ SPI 데이터 수신 완료 (10442 바이트)
[00:00:41.214,599] <wrn> flash_manager: ⚠️ NVS full (-28). Clearing old error logs and retrying...
[00:00:41.441,284] <inf> flash_manager: ✅ Recovered from NVS full.
[00:00:41.441,711] <inf> flash_manager: ✅ Error log saved to flash: device=⚠️ SPI data received OK, error_code=0, timestamp=1769414628 (count: 20/20)
[00:00:41.442,443] <inf> control_tower: =========================>Time: 300 ms, Size: 10433, 받은크기:10442 bytes
[00:00:41.443,878] <inf> lte_mgr: LTE 등록됨(망 등록 상태), PDN 확인 진행
[00:00:41.444,885] <inf> control_tower: 📤 서버로 데이터 전송 중 (10433 바이트) & FOTA 확인...
[00:00:41.778,686] <wrn> flash_manager: ⚠️ NVS full (-28). Clearing old error logs and retrying...
[00:00:41.900,299] <inf> flash_manager: ✅ Recovered from NVS full.
[00:00:41.900,726] <inf> flash_manager: ✅ Error log saved to flash: device=📤 Sending data to OKKKKK, error_code=0, timestamp=1769414629 (count: 20/20)
[00:00:41.901,458] <inf> net_http: ==================net_http_send_lidar_data==================
[00:00:41.904,663] <inf> net_http: ✅ LTE 데이터 추가 완료: 290 바이트
[00:00:41.905,212] <inf> net_http: 📋 HTTP 바디 (처음 400자): {"mac":"78-57-22-31-00-82","seq":1,"ver":"1.0.0+0","temp":0,"hum":0,"batt_chk":10371,"gyro":0,"lidar_time":300,"lidar_op_sec":3559,"tm":1769414629,"lte_data":{"rsrp":-91,"rsrq":-19.5,"snr":-17,"psm_enabled":1,"psm_tau_s":72000,"psm_active_s":20,"usim":"89882280666220532505","plmn":"45006"}
[00:00:41.915,008] <inf> net_http: ✅ 라이다 데이터 인코딩 완료: 10433 바이트 -> 13912 Base64 문자
[00:00:41.915,557] <inf> net_http: HTTP body size: 14238 bytes
[00:00:41.917,297] <inf> net_http: HTTP POST: host=www.feedcheck.kr port=80 path=/view/restSendFeedcheckData_new (body_len=14238)
[00:00:41.918,304] <inf> net_http: DNS 캐시 사용
[00:00:41.918,640] <inf> net_http: LTE 데이터 채널 확인 중...
[00:00:41.919,403] <inf> net_http: ✅ LTE 데이터 채널 준비 완료
[00:00:41.919,799] <inf> net_http: HTTP: 소켓 생성 중...
[00:00:41.920,410] <inf> net_http: HTTP: 연결 중...
[00:00:47.957,519] <inf> lte_mgr: LTE RRC 상태: idle (0)
[00:00:47.957,916] <inf> lte_mgr: LTE PSM 캐시(RRC idle): granted=1 requested=0 tau_s=72000 active_s=20
[00:00:48.368,011] <inf> lte_mgr: LTE RRC 상태: connected (1)
[00:00:48.368,408] <inf> lte_mgr: LTE 복귀 rrc_connected: delta_ms=6925
[00:00:49.509,979] <inf> net_http: ✅ HTTP: 연결 완료 (7589 ms 소요)
[00:00:49.510,498] <inf> net_http: HTTP: 헤더 전송 중 (150 바이트)...
[00:00:49.511,352] <inf> net_http: HTTP: 바디 전송 중 (14238 바이트)...
[00:00:49.828,491] <err> nrf_modem: Modem has crashed, reason 0x10, PC: 0x88426
[00:00:49.829,010] <err> net_http: ❌ 바디 전송 실패 (0/14238 바이트): -110
[00:00:49.829,528] <err> net_http: ❌ 라이다 데이터 전송 실패: -110
[00:00:49.829,956] <err> control_tower: ❌ 서버 전송 실패: -110
[00:00:50.163,726] <wrn> flash_manager: ⚠️ NVS full (-28). Clearing old error logs and retrying...
[00:00:50.285,308] <inf> flash_manager: ✅ Recovered from NVS full.
[00:00:50.285,705] <inf> flash_manager: ✅ Error log saved to flash: device=Server  ERRORRRRRRRR, error_code=-110, timestamp=1769414637 (count: 20/20)
[00:00:50.286,621] <inf> flash_manager: ✅ BATT log saved to flash: timestamp=1769414624, cnt=167, v_ok_idx=3
[00:00:50.287,170] <inf> control_tower: Deep Sleep 준비 완료
[00:00:50.289,825] <inf> control_tower: ℹ️ FOTA 확인: ver='' (현재: 1.0.0+0), url=''
[00:00:50.295,440] <inf> lidar_pwr: ✅ 라이다 전원 OFF
[00:00:50.295,806] <inf> loadswitch: ✅ 로드 스위치 OFF
[00:00:50.296,173] <inf> control_tower: ⏱️ 로드스위치 OFF.
[00:00:50.296,539] <inf> control_tower: ⏱️ PSM 모드 진입 요청

[00:00:50.296,966] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:00:50.297,393] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:00:50.297,790] <err> lte_mgr: PSM 슬립 요청 실패: -14
[00:00:50.298,187] <inf> control_tower: PSM 모드 진입 실패
[00:00:50.298,553] <inf> lte_mgr: RAI 설정 실패: -1
[00:00:50.298,919] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:00:50.299,346] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:00:50.299,774] <err> lte_mgr: PSM sleep request failed: -14
[00:00:50.300,170] <inf> lte_mgr: RAI 설정 실패: -1
[00:00:50.300,537] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:00:50.300,964] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:00:50.301,391] <err> lte_mgr: PSM sleep request failed: -14
[00:00:50.301,788] <inf> control_tower: === ✅ 라이다 측정 시퀀스 완료 ===
[00:00:50.302,246] <inf> control_tower: ========================================================
[00:00:50.402,893] <inf> main: 📅 재시도 스케줄 [0]: +1 분 -> 170458
[00:00:50.403,350] <inf> main: 📅 재시도 스케줄 [1]: +2 분 -> 170558
[00:00:50.403,808] <inf> main: 📅 재시도 스케줄 [2]: +4 분 -> 170758
[00:00:50.404,266] <inf> main: 📅 재시도 스케줄 [3]: +8 분 -> 171158
[00:00:50.404,724] <err> main: 라이다 측정 시퀀스 실패: -14 (재시도 1/4)
[00:01:50.527,862] <inf> control_tower: ========================================================
[00:01:50.528,350] <inf> control_tower: ============= 라이다 측정 시퀀스 시작 =========
[00:01:50.528,808] <inf> control_tower: 펌웨어 버전: 1.0.0+0
[00:01:50.529,205] <inf> control_tower: ========================================================
[00:01:50.529,785] <inf> i2c_vcc_en: ✅ I2C VCC EN: ON (P0.23 = Low)
[00:01:50.630,249] <err> i2c_gyro_sensor: MC3419 device not ready
[00:01:50.630,645] <err> i2c_temp_sensor: AHT20 device not ready
[00:01:50.631,042] <inf> i2c_vcc_en: ✅ I2C VCC EN: OFF (P0.23 = High)
[00:01:50.631,469] <inf> control_tower: 1단계: 로드 스위치 ON
[00:01:50.631,835] <inf> loadswitch: ✅ 로드 스위치 ON
[00:01:50.934,661] <inf> flash_manager: ✅ BATT log loaded from flash: timestamp=1769414624, cnt=167
[00:01:50.935,272] <inf> control_tower: 2단계: 배터리 전압 10.0V 이상 대기 중...
[00:01:51.238,647] <inf> control_tower: 3단계: 라이다 전원 ON
[00:01:51.252,929] <inf> lidar_spi: ✅ SPI 트랜잭션 복구 완료
[00:01:51.263,397] <inf> lidar_pwr: ✅ 라이다 전원 ON
[00:01:51.763,824] <inf> control_tower: ⏳ SPI 데이터 수신 대기 중...
[00:01:54.781,158] <inf> control_tower: 3단계: 라이다 전원 OFF
[00:01:54.786,682] <inf> lidar_pwr: ✅ 라이다 전원 OFF
[00:01:54.791,656] <inf> flash_manager: ✅ BATT log saved to flash: timestamp=1769414698, cnt=168, v_ok_idx=3
[00:01:54.792,236] <inf> control_tower: ✅ SPI 데이터 수신 완료 (10442 바이트)
[00:01:55.121,154] <wrn> flash_manager: ⚠️ NVS full (-28). Clearing old error logs and retrying...
[00:01:55.247,375] <inf> flash_manager: ✅ Recovered from NVS full.
[00:01:55.247,772] <inf> flash_manager: ✅ Error log saved to flash: device=⚠️ SPI data received OK, error_code=0, timestamp=1769414702 (count: 20/20)
[00:01:55.248,535] <inf> control_tower: =========================>Time: 300 ms, Size: 10433, 받은크기:10442 bytes
[00:01:55.249,114] <err> lte_lc: Could not get registration status, error: -1
[00:01:55.249,572] <wrn> lte_mgr: LTE 등록 상태 조회 실패: -14, 재접속
[00:01:55.250,000] <inf> lte_mgr: LTE 연결 시도 1
[00:01:55.250,366] <err> lte_lc: Failed to get system mode, error: -1
[00:01:55.250,793] <wrn> lte_mgr: LTE 모드[pre_connect]: 시스템 조회 실패: -14
[00:01:55.251,251] <err> lte_lc: Could not get the LTE mode, error: -1
[00:01:55.251,678] <wrn> lte_mgr: LTE 모드[pre_connect]: LTE 모드 조회 실패: -14
[00:01:55.252,166] <err> lte_lc: AT command failed, nrf_modem_at_scanf() returned error: -1
[00:01:55.252,624] <wrn> lte_mgr: LTE 모드[pre_connect]: 기능 모드 조회 실패: -14
[00:01:55.253,143] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:01:55.253,570] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:01:55.253,967] <wrn> lte_mgr: PSM 요청 초기 설정 실패: -14
[00:01:55.254,394] <err> lte_lc: Could not get registration status, error: -1
[00:01:55.254,821] <err> lte_lc: Failed to get current registration status
[00:01:55.255,249] <err> lte_mgr: LTE 비동기 연결 실패: -14
[00:01:55.255,676] <wrn> lte_mgr: LTE 연결 실패: -14, 백오프 5초

[00:02:00.256,164] <inf> lte_mgr: LTE 연결 시도 2
[00:02:00.256,530] <err> lte_lc: Failed to get system mode, error: -1
[00:02:00.256,958] <wrn> lte_mgr: LTE 모드[pre_connect]: 시스템 조회 실패: -14
[00:02:00.257,415] <err> lte_lc: Could not get the LTE mode, error: -1
[00:02:00.257,843] <wrn> lte_mgr: LTE 모드[pre_connect]: LTE 모드 조회 실패: -14
[00:02:00.258,331] <err> lte_lc: AT command failed, nrf_modem_at_scanf() returned error: -1
[00:02:00.258,789] <wrn> lte_mgr: LTE 모드[pre_connect]: 기능 모드 조회 실패: -14
[00:02:00.259,307] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:02:00.259,735] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:02:00.260,131] <wrn> lte_mgr: PSM 요청 초기 설정 실패: -14
[00:02:00.260,559] <err> lte_lc: Could not get registration status, error: -1
[00:02:00.260,986] <err> lte_lc: Failed to get current registration status
[00:02:00.261,383] <err> lte_mgr: LTE 비동기 연결 실패: -14
[00:02:00.262,237] <wrn> lte_mgr: LTE 연결 실패: -14, 백오프 10초
[00:02:10.262,756] <inf> lte_mgr: LTE 연결 시도 3
[00:02:10.263,122] <err> lte_lc: Failed to get system mode, error: -1
[00:02:10.263,549] <wrn> lte_mgr: LTE 모드[pre_connect]: 시스템 조회 실패: -14
[00:02:10.264,007] <err> lte_lc: Could not get the LTE mode, error: -1
[00:02:10.264,434] <wrn> lte_mgr: LTE 모드[pre_connect]: LTE 모드 조회 실패: -14
[00:02:10.264,923] <err> lte_lc: AT command failed, nrf_modem_at_scanf() returned error: -1
[00:02:10.265,411] <wrn> lte_mgr: LTE 모드[pre_connect]: 기능 모드 조회 실패: -14
[00:02:10.265,899] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:02:10.266,326] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:02:10.266,723] <wrn> lte_mgr: PSM 요청 초기 설정 실패: -14
[00:02:10.267,150] <err> lte_lc: Could not get registration status, error: -1
[00:02:10.267,578] <err> lte_lc: Failed to get current registration status
[00:02:10.268,005] <err> lte_mgr: LTE 비동기 연결 실패: -14
[00:02:10.268,432] <err> control_tower: LTE 재연결 실패 (3회 시도 후): -14
[00:02:10.597,320] <wrn> flash_manager: ⚠️ NVS full (-28). Clearing old error logs and retrying...
[00:02:10.723,693] <inf> flash_manager: ✅ Recovered from NVS full.
[00:02:10.724,121] <inf> flash_manager: ✅ Error log saved to flash: device=LTE reconnection failed, error_code=-14, timestamp=1769414718 (count: 20/20)
[00:02:10.725,036] <inf> flash_manager: ✅ BATT log saved to flash: timestamp=1769414698, cnt=168, v_ok_idx=3
[00:02:10.725,585] <inf> control_tower: Deep Sleep 준비 완료
[00:02:10.728,210] <inf> control_tower: ℹ️ FOTA 확인: ver='' (현재: 1.0.0+0), url=''
[00:02:10.733,795] <inf> lidar_pwr: ✅ 라이다 전원 OFF
[00:02:10.734,161] <inf> loadswitch: ✅ 로드 스위치 OFF
[00:02:10.734,527] <inf> control_tower: ⏱️ 로드스위치 OFF.
[00:02:10.734,924] <inf> control_tower: ⏱️ PSM 모드 진입 요청
[00:02:10.735,321] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:02:10.735,778] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:02:10.736,175] <err> lte_mgr: PSM 슬립 요청 실패: -14
[00:02:10.736,572] <inf> control_tower: PSM 모드 진입 실패
[00:02:10.736,938] <inf> lte_mgr: RAI 설정 실패: -1
[00:02:10.737,304] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:02:10.737,762] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:02:10.738,159] <err> lte_mgr: PSM sleep request failed: -14
[00:02:10.738,555] <inf> lte_mgr: RAI 설정 실패: -1
[00:02:10.738,922] <err> lte_lc: nrf_modem_at_printf failed, reported error: -1
[00:02:10.739,349] <err> lte_mgr: PSM 요청 활성화 실패: -14
[00:02:10.739,746] <err> lte_mgr: PSM sleep request failed: -14
[00:02:10.740,142] <inf> control_tower: === ✅ 라이다 측정 시퀀스 완료 ===
[00:02:10.740,570] <inf> control_tower: ========================================================
[00:02:10.741,058] <err> main: 라이다 측정 시퀀스 실패: -14 (재시도 2/4)