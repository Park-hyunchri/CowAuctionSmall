# 축산물품질평가원 OpenAPI활용가이드 축산물통합이력정보조회 v2 10

> DOCX에서 변환한 Markdown 문서입니다. [원본 문서](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10.docx). 원문의 문구·표·예제를 보존했습니다. 원본 목차의 페이지 번호는 DOCX 기준이며, 병합된 표 셀은 Markdown에서 빈 셀로 펼쳤습니다.

축산물품질평가원

![Document image 1](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image1.png)

![Document image 2](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image2.png)

![Document image 3](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image3.png)

![Document image 4](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image4.png)

축산물통합이력정보 서비스

OpenAPI 활용가이드

![Document image 5](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image5.png)

## 문서 정보

| 문서명 | 축산물품질평가원 축산물통합이력정보 서비스 OpenAPI활용가이드 |
| --- | --- |

## 개정 이력

| 버 전 | 변경일 | 변경 사유 | 변경 내용 | 작성자 | 승인 |
| --- | --- | --- | --- | --- | --- |
| 1.0 | 2015-05-12 | 최초작성 | 최초작성 | 정강모 |  |
|  | 2015-06-26 | 내용수정 | 응답메세지 결과 세분화 | 정강모 |  |
|  | 2015-07-02 | 항목추가 | 묶음번호 구성내역 조회시 도축장정보 추가 | 정강모 |  |
|  | 2015-08-12 | 내용수정 | 서비스 사용 및 명세 수정 | 정강모 |  |
|  | 2015-11-03 | 항목추가 | 소 개체번호 조회시 농장 농장식별번호 추가 | 정강모 |  |
| 2.3 | 2016-03-16 | 내용수정 | `16년 오픈API 품질진단 결과내역 반영 | 권윤길 | 유송원 |
| 2.4 | 2017-09-27 | 문서 현행화 | 오타 및 요청/응답 메시지 수정 | 권윤길 | 유송원 |
| 2.5 | 2018-09-17 | 문서 현행화 | `18년 오픈API 품질진단 결과내역 반영 | 김영준 | 김은미 |
| 2.6 | 2019-02-20 | 내용 추가 | 소 이력번호 조회시 농장번호 항목 추가 |  |  |
|  | 2019-04-10 | 내용 추가 | 축산물이력정보조회 응답메시지 insfat(근내지방도) 항목추가 | 김경민 |  |
| 2.7 | 2020-04-27 | 항목추가 | 가금(닭,오리 이력번호/묶음번호, 계란 이력번호) 추가 | 김경민 |  |
|  | 2020-10-07 | 내용수정 | 서비스 Key 발급 현행화 | 김경민 |  |
| 2.8 | 2022-08-11 | 내용 추가 | (infoType : 7)<br>결핵 검사일자 및 결학 검사결과 추가<br>브루셀라 접종일 -&gt; 검사일 명칭변경 | 김경민 |  |
|  | 2022-08-22 | 신규 추가 | 농장식별번호 검색 API 신규추가 | 김경민 |  |
| 2.10 | 2025-03-05 | 항목 추가 | 럼피스킨 백신 최종 접종일자 항목 추가 | 김지영 | 권기문 |

목 차목 차

- 1. 서비스 사용	4

- 1.1. 서비스 Key 발급 및 활용	4

- 가. 서비스 Key발급(인증신청 및 발급)	4

- 나. 서비스 인증키 활용	9

- 2. 서비스 목록	9

- 3. 서비스 명세	10

- 3.1. 축산물 이력정보 서비스	10

- 가. 서비스 개요	10

- 나. API  목록	11

## 서비스 사용

### 서비스 Key 발급 및 활용

### 서비스 Key발급(인증신청 및 발급)

#### 공공데이터포털(http://data.go.kr) 활용자

서비스키 발급 및 활용신청은 공공데이터포털 홈페이지에서 가입 및 로그인후 진행가능

![Document image 6](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image6.png)

활용신청하고자 하는 API를 검색하여 활용신청 버튼을 클릭.

![Document image 7](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image7.png)

활용신청하고자 하는 내역을 입력하고, 활용할 API를 선택하여 활용신청 버튼을 클릭

![Document image 8](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image8.png)

신청이 완료되면 신청목록이 조회되고 승인이 완료된 건을 클릭하면 인증키를 확인할 수 있다

![Document image 9](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image9.png)

개발 계정 신청이 승인 되었을 경우 인증키를 발급받을수 있으며 인증키가 없을 경우 인증키 발급 버튼이 나타나며, 인증키가 발급되어 있을 경우에는 변경 신청 및 연장 신청 버튼등이 보여진다. (축산물품질평가원에서 제공하는 오픈API는 자동승인이기 때문에 신청후 인증키는 자동으로 발급되며 신청 후 약 한시간이 지난후에 사용가능. 이는 공공데이터포털 동기화로 인한 부분이며 신청 후 한시간 이상이 지났는데도 사용이 되지 않을경우 공공데이터포털 측으로 문의필요)

![Document image 10](%EC%B6%95%EC%82%B0%EB%AC%BC%ED%92%88%EC%A7%88%ED%8F%89%EA%B0%80%EC%9B%90_OpenAPI%ED%99%9C%EC%9A%A9%EA%B0%80%EC%9D%B4%EB%93%9C_%EC%B6%95%EC%82%B0%EB%AC%BC%ED%86%B5%ED%95%A9%EC%9D%B4%EB%A0%A5%EC%A0%95%EB%B3%B4%EC%A1%B0%ED%9A%8C_v2_10_assets/image10.png)

### 서비스 인증키 활용

#### REST 방식의 인증키(UTF-8) 활용

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace?ServiceKey=인증키
```

#### SOAP 방식의 인증키 활용

**요청 메시지**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:head="http://apache.org/headers" xmlns:soap="http://soap.open.pp.acrc/">
<soapenv:Header>
<head:ComMsgHeader>
<ServiceKey> 인증키</ServiceKey>
</head:ComMsgHeader>
</soapenv:Header>
<soapenv:Body>
<soap:getOpenProposalList>
<OpenPropListRequest>
<nowPageNo>1</nowPageNo>
<pageMg>2</pageMg>
< region>5%EC%9B%9420101101</ region>
<regTo>20101130</regTo>
</OpenPropListRequest>
</soap:getOpenProposalList>
</soapenv:Body>
</soapenv:Envelope>
```

## 서비스 목록

| 순번 | 서비스 ID | 서비스명(영문) | 서비스명(국문) |
| --- | --- | --- | --- |
| 1 | SC-OA-21-09 | AnimalTrace | 축산물통합이력정보조회 서비스 |

## 서비스 명세

### 축산물 이력정보 서비스

### 서비스 개요

| 서비스 정보 | 서비스 ID | SC-OA-21-09 |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- | --- |
|  | 서비스명(국문) | 축산물통합이력정보조회 서비스 |  |  |  |  |  |
|  | 서비스명(영문) | AnimalTrace |  |  |  |  |  |
|  | 서비스 설명 | 소·돼지 이력번호, 묶음번호를 통해 사육(출생일자, 소의 품종, 성별), 이동(농장, 이동구분,  사육지), 도축(도축장, 도축일자, 도축검사정보, 등급), 유통정보(포장처리일자, 업소명, 판매장) 등을 조회는 ②축산물통합이력정보 제공 기능을 제공하는 ③축산물 이력정보통합조회 서비스 |  |  |  |  |  |
| 서비스 보안 | 서비스 인증/권한 | [] 서비스 Key[ ] 인증서 (GPKI)<br>[] Basic (ID/PW) [O] 없음 |  |  |  |  | [ ]WS-Security |
|  | 메시지 레벨 암호화 | [  ] 전자서명 [ ] 암호화 [O] 없음 |  |  |  |  |  |
|  | 전송 레벨 암호화 | [  ] SSL   [O] 없음 |  |  |  |  |  |
| 적용 기술 수준 | 인터페이스 표준 | [  ] SOAP 1.2<br>(RPC-Encoded, Document Literal, Document Literal Wrapped)<br>[ O ] REST (GET)<br>[ ] RSS 1.0 [ ] RSS 2.0 [ ] Atom 1.0 [ ] 기타 |  |  |  |  |  |
|  | 교환 데이터 표준 | [ O ] XML [ ] JSON [ ] MIME [ ] MTOM |  |  |  |  |  |
| 서비스 URL | 개발환경 | http://data.ekape.or.kr/openapi-data/service/user/animalTrace |  |  |  |  |  |
|  | 운영환경 | http://data.ekape.or.kr/openapi-data/service/user/animalTrace |  |  |  |  |  |
| 서비스 WADL | 개발환경 | http://data.ekape.or.kr/openapi-data/service/user/animalTrace?_wadl&amp;type=xml |  |  |  |  |  |
|  | 운영환경 | http://data.ekape.or.kr/openapi-data/service/user/animalTrace?_wadl&amp;type=xml |  |  |  |  |  |
| 서비스 배포 정보 | 서비스 버전 | 2.5 |  |  |  |  |  |
|  | 유효일자 | 2018.09.27 |  | 배포 일자 |  |  | 2017.09.26 |
|  | 서비스 이력 | 데이터 일제정비 |  |  |  |  |  |
| 메시지 교환 유형 |  | [ O ] Request-Response [ ] Publish-Subscribe<br>[ ] Fire-and-Forgot  [ ] Notification |  |  |  |  |  |
| 메시지 로깅 수준 |  | 성공 | [O] Header [O] Body |  | 실패 | [O] Header [] Body |  |
| 사용 제약 사항 (비고) |  |  |  |  |  |  |  |

### API  목록

| 일련번호 | 서비스명(국문) | API 명(영문) | API 명(국문) |
| --- | --- | --- | --- |
| 1 | 축산물 이력정보통합조회 서비스 | /traceNoSearch | 축산물이력정보조회 |
| 2 | 축산물 농장식별번호 조회 서비스 | /farmUniqueNoSearch | 축산물농장식별번호조회 |

#### [축산물 이력(묶음)정보조회] API

| API  정보 | API  번호 | 1 | API 명(국문) | 축산물이력정보조회 |
| --- | --- | --- | --- | --- |
|  | API  유형 | 조회 | API 명(영문) | /traceNoSearch |
|  | API 상세기능 설명 | 축산물 이력(묶음)정보를 조회하는 기능 제공 |  |  |
|  | API 서비스 제공자 | 백형민 / 공공데이터포털 운영 / 044)410-7146 / carakom@ekape.or.kr |  |  |
|  | API 데이터 갱신주기 | 실시간 |  |  |
|  | Call Back URL | http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch |  |  |
|  | 최대 메시지 사이즈 | [   bytes] |  |  |
|  | 평균 응답 시간 | [   ms] | 초당 최대 트랜잭션 | [  tps] |

##### 요청 메시지 명세

| 항목명(영문) | 항목명(국문) | 항목크기 | 항목구분 | 샘플데이터 | 항목설명 |
| --- | --- | --- | --- | --- | --- |
| traceNo | 이력(묶음)번호 | 20 | 1 | L01709271277007 | 개체/이력/묶음번호 |
| optionNo | 옵션번호 | 10 | 0 | 9 | 조회하려는 정보 옵션값<br>1~7 : 개체/이력, 8~9 : 묶음<br>1 : 개체(소), 사육(돼지)<br>2 : 출생 등 신고(소)<br>3 : 도축(소/돼지)<br>4 : 포장(소/돼지)<br>5 : 구제역백신(소)<br>6 : 질병정보(소)<br>7 : 브루셀라(소)<br>8 : 묶음 기본정보(묶음)<br>9 : 묶음구성내역(묶음)<br>-----------------------------<br>가금 &gt; 닭,오리 &gt; 이력<br>1 : 사육정보<br>2 : 도축정보<br>3 : 식육포장처리정보<br>가금 &gt; 닭,오리 &gt; 묶음<br>1 : 묶음정보<br>2 : 식육포장처리정보<br>3 : 이력정보<br>가금 &gt; 계란<br>4 : 식용란 선별포장처리 정보<br>5 : 식용란 수집판매 정보 |
| corpNo | 사업자번호 | 10 | 0 | 1178522046 | 묶음을 구성한 업소의 사업자 번호 |

※ 항목구분 : 필수(1), 옵션(0), 1건 이상 복수건(1..n), 0건 또는 복수건(0..n)

##### 응답 메시지 명세

| 항목명(영문) | 항목명(국문) | 항목크기 | 항목구분 | 샘플데이터 | 항목설명 |
| --- | --- | --- | --- | --- | --- |
| 공통 |  |  |  |  |  |
| traceNoType | 소/돼지/묶음 구분 | 20 | 1 | CATTLE/LOT_NO | CATTLE/CATTLE_NO : 소 개체<br>CATTLE/LOT_NO : 소 묶음<br>PIG/PIG_NO : 돼지 이력<br>PIG/LOT_NO : 돼지 묶음<br>FOWL/HIST_NO : 닭 이력<br>DUCK/HIST_NO : 오리 이력<br>EGG/HIST_NO : 계란 이력<br>FOWL/LOT_NO : 닭 묶음<br>DUCK/LOT_NO : 오리 묶음 |
| infoType | 조회정보 | 1 | 1 | 9 | 조회 정보 옵션값<br>1~7 : 개체/이력, 8~9 : 묶음<br>1 : 개체(소), 사육(돼지)<br>2 : 출생 등 신고(소)<br>3 : 도축(소/돼지)<br>4 : 포장(소/돼지)<br>5 : 구제역백신(소)<br>6 : 질병정보(소)<br>7 : 브루셀라(소)<br>8 : 묶음 기본정보(묶음)<br>9 : 묶음구성내역(묶음)<br>-----------------------------<br>가금 &gt; 닭,오리 &gt; 이력<br>1 : 사육정보<br>2 : 도축정보<br>3 : 식육포장처리정보<br>가금 &gt; 닭,오리 &gt; 묶음<br>1 : 묶음정보<br>2 : 식육포장처리정보<br>3 : 이력정보<br>가금 &gt; 계란<br>4 : 식용란 선별포장처리 정보<br>5 : 식용란 수집판매 정보 |
| traceNoType : CATTLE/CATTLE_NO |  |  |  |  |  |
| infoType : 1 |  |  |  |  |  |
| birthYmd | 출생일자 | 10 | 1 | 20120317 | 출생년월일 |
| cattleNo | 소 개체번호 | 15 | 1 | 410002075264204 | 이력번호 |
| flatEartagNo | 재귀표번호 | 10 | 0 |  | 재부착번호 |
| lsTypeNm | 소의 종류 | 20 | 1 | 한우 | 소의 종류 |
| monthDiff | 수입경과월 | 10 | 0 | 0 | 수입경과월 |
| nationNm | 수입국가 | 20 | 0 |  | 수입국가 |
| sexNm | 성별 | 10 | 1 | 암 | 성별 |
| farmUniqueNo | 농장식별번호 | 6 | 0 | 211058 | 농장식별번호 |
| farmNo | 농장번호 | 10 | 0 | 122992 | 농장번호 |
| lsdYmd | 럼피스킨 최종접종일 | 8 | 0 | 20250218 | 럼피스킨 최종접종일 |
| infoType : 2 |  |  |  |  |  |
| farmAddr | 사육지 | 200 | 1 | 강원도 원주시 호저면 | 사육지 |
| farmerNm | 소유주 | 100 | 1 | 고정남 | 소유주 |
| regType | 신고구분 | 20 | 1 | 전산등록 | 신고구분 |
| regYmd | 년월일 | 8 | 1 | 20120319 | 년월일 |
| farmNo | 농장번호 | 10 | 0 | 122992 | 농장번호 |
| infoType : 3 |  |  |  |  |  |
| butcheryPlaceAddr | 도축장 주소 | 200 | 1 | 경기도 안성시 일죽면 일생로 | 도축장 주소 |
| butcheryPlaceNm | 도축장명 | 100 | 1 | 도드람LPC | 도축장명 |
| butcheryYmd | 도축일자 | 8 | 1 | 20170920 | 도축일자 |
| gradeNm | 등급명 | 20 | 0 | 1 | 육질등급 |
| insfat | 근내지방도 | 20 | 0 | 9 | 근내지방도<br>(육질등급이 1++인 경우에만 데이터 있음) |
| inspectPassYn | 위생검사 결과 | 1 | 0 | 합격 | 합격,<br>불합격<br>보류 |
| infoType : 4 |  |  |  |  |  |
| processPlaceAddr | 포장처리업소 주소 | 200 | 1 | 경기도 광주시 곤지암읍 경충대로 | 포장처리업소 주소 |
| processPlaceNm | 포장처리업소명 | 50 | 1 | 우성육가공 | 포장처리업소명 |
| infoType : 5 |  |  |  |  |  |
| injectionDayCnt | 구제역 백신접종경과일 | 200 | 0 | 접종 후 2일 경과 | 구제역 백신접종경과일 |
| injectionYmd | 구제역 백신접종일 | 8 | 0 | 20170918 | 구제역 예방접종최종일자 |
| vaccineorder | 구제역 백신접종 차수 | 10 | 0 | 13차 | 구제역 백신접종 차수 |
| infoType : 6 |  |  |  |  |  |
| inspectDesc | 질병유무 | 100 | 0 |  | 질병유무 |
| infoType : 7 |  |  |  |  |  |
| inspectDt | 브루셀라 검사일 | 8 | 0 | 20170911 | 브루셀라 검사최종일자 ※ 실시간으로 조회되지 않아 정보가 다를 수 있습니다. |
| inspectYn | 브루셀라 접종 유무 | 10 | 0 | 음성 | 브루셀라 검사결과 |
| tbcInspectYmd | 결핵 검사일 | 8 | 0 | 20170911 | 결핵 검사최종일자 |
| tbcInspectRsltNm | 결핵 검사결과 | 10 | 0 | 음성 | 결핵 검사결과 |
| traceNoType : CATTLE/LOT_NO |  |  |  |  |  |
| infoType : 8 |  |  |  |  |  |
| corpNo | 사업자번호 | 10 | 1 | 1178522046 | 사업자번호 |
| lotNo | 묶음번호 | 30 | 1 | L01709271277007 | 묶음번호 |
| processPlaceAddr | 묶음구성업소 주소 | 200 | 1 | 서울특별시 양천구 목동동로 257 | 묶음구성업소 주소 |
| processPlaceNm | 묶음구성업소명 | 50 | 1 | ㈜현대그린푸드목동점 | 묶음구성업소명 |
| infoType : 9 |  |  |  |  |  |
| cattleNo | 소 개체번호 | 15 | 1 | 410002075264204 | 소 개체번호 |
| corpNo | 사업자번호 | 10 | 1 | 1178522046 | 사업자번호 |
| farmAddr | 농장주소 | 200 | 1 | 강원도 원주시 호저면 매호리 | 농장주소 |
| gradeNm | 등급명 | 20 | 1 | 1 | 등급명 |
| insfat | 근내지방도 | 20 | 0 | 9 | 근내지방도<br>(등급이 1++인 경우에만 데이터 있음) |
| lotNo | 묶음번호 | 30 | 1 | L01709271277007 | 묶음번호 |
| lsTypeNm | 소의 종류 | 20 | 1 | 한우 | 소의 종류 |
| processPlaceAddr | 묶음구성업소 주소 | 200 | 1 | 서울특별시 양천구 목동 916번지 | 포장처리업소 주소 |
| processPlaceNm | 묶음구성업소명 | 50 | 1 | ㈜현대그린푸드목동점 | 묶음구성업소명 |
| butcheryPlaceAddr | 도축장 주소 | 200 | 1 | 경기도 안성시 일죽면 금산리 598번지 | 도축장 주소 |
| butcheryPlaceNm | 도축장명 | 100 | 1 | 도드람LPC | 도축장명 |
| butcheryYmd | 도축일자 | 8 | 1 | 20170920 | 도축일자 |
|  |  |  |  |  |  |
| traceNoType : PIG/PIG_NO |  |  |  |  |  |
| infoType : 1 |  |  |  |  |  |
| farmAddr | 농장소재지 | 200 | 1 | 경상북도 포항시 북구 청하면 비학로 | 농장소재지 |
| farmerNm | 농장경영자 | 100 | 1 | 강충열 | 농장경영자 |
| farmUniqueNo | 농장식별번호 | 6 | 1 | 700030 | 농장식별번호 |
| pigNo | 이력번호 | 20 | 1 | 170003000058 | 이력번호 |
| infoType : 3 |  |  |  |  |  |
| butcheryPlaceAddr | 도축장 주소 | 200 | 1 | 경상북도 영천시 동강포길 29-5 | 도축장 주소 |
| butcheryPlaceNm | 도축장명 | 100 | 1 | (주)삼세 | 도축장명 |
| butcheryYmd | 도축일자 | 8 | 1 | 20150730 | 도축일자 |
| inspectPassYn | 위생검사 결과 | 1 | 0 | Y | 합격 : Y,<br>불합격 : N<br>보류 : H |
| pigNo | 돼지 이력번호 | 20 | 1 | 170003000058 | 돼지 이력번호 |
| infoType : 4 |  |  |  |  |  |
| gradeNm | 등급명 | 20 | 0 |  | 등급명 |
| pigNo | 돼지 이력번호 | 20 | 1 | 170003000058 | 돼지 이력번호 |
| processPlaceAddr | 포장처리업소 주소 | 200 | 1 | 경상남도 양산시 상북면 ,지하1층 | 포장처리업소 주소 |
| butcheryPlaceNm | 도축장명 | 100 | 1 | ㈜산청푸드 | 도축장명 |
| processYmd | 포장처리일자 | 8 | 1 | 20150731 | 포장처리일자 |
| traceNoType : PIG/LOT_NO |  |  |  |  |  |
| infoType : 8 |  |  |  |  |  |
| corpNo | 사업자번호 | 10 | 1 | 6218176868 | 사업자번호 |
| gradeNm | 등급명 | 20 | 0 |  | 등급명 |
| lotNo | 묶음번호 | 30 | 1 | L11507319120001 | 묶음번호 |
| processPlaceAddr | 포장처리업소 소재지 | 200 | 1 | 경상남도 양산시 상북면 충렬로 574, 지하1층 | 포장처리업소 소재지 |
| processPlaceNm | 포장처리업소명 | 50 | 1 | ㈜산청푸드 | 포장처리업소명 |
| processYmd | 포장처리일자 | 8 | 1 | 20150731 | 포장처리일자 |
| infoType : 9 |  |  |  |  |  |
| corpNo | 사업자번호 | 10 | 1 | 1992484871 | 사업자번호 |
| farmAddr | 농장 소재지 | 200 | 1 | 경상북도 포항시 북구 청하면 비학로 | 농장 소재지 |
| farmerNm | 사육자명 | 100 | 1 | 강충열 | 사육자명 |
| pigNo | 이력(묶음)번호 | 20 | 1 | 170003000058 | 이력(묶음)번호<br>* 재묶음인경우 묶음번호일수 있습니다. |
| butcheryPlaceAddr | 도축장 주소 | 200 | 1 | 경상북도 영천시 도남동 695번지 | 도축장 주소 |
| butcheryPlaceNm | 도축장명 | 100 | 1 | (주)삼세 | 도축장명 |
| butcheryYmd | 도축일자 | 8 | 1 | 20150730 | 도축일자 |
| traceNoType : FOWL/HIST_NO &amp; DUCK/HIST_NO |  |  |  |  |  |
| infoType : 1 |  |  |  |  |  |
| farmAddr | 농장소재지 | 100 | 1 | 전라북도 고창군 부안면 | 농장소재지 |
| histNo | 이력번호 | 12 | 1 | 5 200416 040 01 | 이력번호(첫번째 자리가 2인 경우는 닭,  3인 경우는 계란,<br>5인경우는 오리) |
| farmIdno | 농장식별번호 | 6 | 1 | 503317 | 농장식별번호 |
| mngrNm | 농장경영자명 | 50 | 0 | 정균석 | 농장경영자명 |
| brdngEnvrnNm | 사육환경 | 30 | 1 | 2(축사내 평사) | 사육환경 |
| lvindRgrfc | 축산업 등록번호 | 5 | 0 | GGJJR | 축산업 등록번호 |
| infoType : 2 |  |  |  |  |  |
| histNo | 이력번호 | 12 | 1 | 5 200416 040 01 | 이력번호(첫번째 자리가 2인 경우는 닭,  3인 경우는 계란,<br>5인경우는 오리) |
| abattNm | 작업장명 | 50 | 1 | (주)삼호유황오리 | 작업장명 |
| add0R | 도축장 소재지 | 100 | 1 | 전라북도 정읍시 고부면 | 도축장 소재지 |
| rceptDt | 도축일자 | 8 | 1 | 2020-04-16 | 도축일자 |
| psexmYn | 도축검사결과 | 4 | 1 | 합격 | 도축검사결과 |
| infoType : 3 |  |  |  |  |  |
| histNo | 이력번호 | 12 | 1 | 5 200416 040 01 | 이력번호(첫번째 자리가 2인 경우는 닭,  3인 경우는 계란,<br>5인경우는 오리) |
| entrpNm | 업체 명 | 50 | 1 | (주)삼호유황오리 | 업체 명 |
| add0R | 업체주소 | 100 | 1 | 전라북도 정읍시 고부면 | 업체주소 |
| traceNoType : EGG/HIST_NO |  |  |  |  |  |
| infoType : 4 |  |  |  |  |  |
| histNo | 이력번호 | 12 | 1 | 3 0416 007 0002 | 이력번호 |
| frmrNm | 농장 경영자 명 | 30 | 1 | 제주초록영농조합법인 | 농장 경영자 명 |
| reqerEntrpNm | 신청인 업체 명 | 50 | 0 | 제주웰빙영농조합법인 | 신청인 업체 명 |
| reqerEntrpAddr | 신청인 업체 주소 | 100 | 1 | 제주특별자치도 제주시 애월읍 | 신청인 업체 주소 |
| issueDt | 발급 일자 | 8 | 1 | 2020-04-16 | 발급 일자 |
| edygOvpstDt | 식용란 산란 일자 | 8 | 1 | 2020-04-15 | 식용란 산란 일자 |
| farmIdno | 농장식별번호 | 6 | 1 | 900610 | 농장식별번호 |
| brdngEnvrnNm | 사육환경 | 30 | 1 | 3(개선된 케이지,0.075㎡/마리) | 사육환경 |
| lvindRgrfc | 축산업 등록번호 | 5 | 0 | WSZQ1 | 축산업 등록번호 |
| successYn | 선별검사결과 | 4 | 1 | 합격 | 선별검사결과 |
| infoType : 5 |  |  |  |  |  |
| histNo | 이력번호 | 12 | 1 | 3 0416 007 0002 | 이력번호 |
| entrpAddr | 거래처 소재지 | 100 | 1 | 제주특별자치도 제주시 애월읍 | 거래처 소재지 |
| entrpNm | 거래처 업체 명 | 50 | 1 | 제주웰빙영농조합법인 | 거래처 업체 명 |
| traceNoType : FOWL/LOT_NO &amp; DUCK/LOT_NO |  |  |  |  |  |
| infoType : 1 |  |  |  |  |  |
| lotNo | 묶음 번호 | 15 | 1 | L22004152182001 | 묶음 번호(앞 두자리가 L2 인경우는 닭, L5인 경우는 오리) |
| entrpNm | 업체 명 | 50 | 1 | 농협목우촌 | 업체 명 |
| add0R | 업체주소 | 100 | 1 | 충청북도 음성군 금왕읍 | 업체주소 |
| infoType : 2 |  |  |  |  |  |
| lotNo | 묶음번호 | 15 | 1 | L22004152182001 | 묶음번호 |
| entrpNm | 업체 명 | 50 | 1 | (주)한국육계유통(묶음) | 업체 명 |
| add0R | 업체주소 | 100 | 1 | 충청남도 당진시 송악읍 | 업체주소 |
| infoType : 3 |  |  |  |  |  |
| farmAddr | 농장소재지 | 100 | 1 | 경상북도 봉화군 법전면 | 농장소재지 |
| lotNo | 묶음 번호 | 15 | 1 | L22004152182001 | 묶음 번호 |
| histNo | 이력번호 | 12 | 1 | 220041503401 | 이력번호 |
| mngrNm | 농장주 명 | 30 | 1 | 이윤재 | 농장주 명 |

※ 공통 응답메세지와 traceNoType, infoType에 따라 해당 컬럼이 출력됩니다.

※ 항목구분은 infoType이 나오는 경우에 해당합니다.

※ 항목구분 : 필수(1), 옵션(0), 1건 이상 복수건(1..n), 0건 또는 복수건(0..n)

※ 닭,오리 이력번호 조회의 경우에는 출력되는 데이터만 다를뿐 항목은 동일하기에 FOWL/HIST_NO &amp; DUCK/HIST_NO 로 표기하였습니다. (묶음번호(LOT_NO)의 경우에도 동일합니다.)

##### 요청 / 응답 메시지 예제

###### traceNoType : CATTLE/CATTLE_NO

**REST(URI)**

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch?ServiceKey=인증키 &traceNo=002075264204&optionNo=9&corpNo=1178522046
```

**응답 메시지**

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>
- <response>
- <header>
<resultCode>00</resultCode>
<resultMsg>NORMAL SERVICE.</resultMsg>
</header>
<notice />
- <body>
- <items>
- <item>
<birthYmd>20130712</birthYmd>
<cattleNo>410002086431813</cattleNo>
<infoType>1</infoType>
<lsTypeNm>육우</lsTypeNm>
<monthDiff>0</monthDiff>
<sexNm>거세</sexNm>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
<farmUniqueNo>CATTLE|CATTLE_NO</farmUniqueNo>
<farmNo>342267</farmNo>
</item>
- <item>
<farmAddr>경상북도 의성군 구천면</farmAddr>
<farmerNm>지태용</farmerNm>
<infoType>2</infoType>
<regType>전산등록</regType>
<regYmd>20130716</regYmd>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
<farmNo>296064</farmNo>
<lsdYmd>20250101</lsdYmd>
</item>
- <item>
<farmAddr>대구광역시 달성군 하빈면</farmAddr>
<farmerNm>정미란</farmerNm>
<infoType>2</infoType>
<regType>양수</regType>
<regYmd>20130812</regYmd>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
<farmNo>296064</farmNo>
</item>
- <item>
<farmAddr>경상북도 구미시 선산읍</farmAddr>
<farmerNm>안승재</farmerNm>
<infoType>2</infoType>
<regType>양수</regType>
<regYmd>20130926</regYmd>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
<farmNo>344678</farmNo>
</item>
- <item>
<farmAddr>경상북도 김천시 아포읍</farmAddr>
<farmerNm>이병호</farmerNm>
<infoType>2</infoType>
<regType>양수</regType>
<regYmd>20131204</regYmd>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
<farmNo>209701</farmNo>
</item>
- <item>
<farmAddr>경상북도 김천시 대항면</farmAddr>
<farmerNm>금오산 낙농축협</farmerNm>
<infoType>2</infoType>
<regType>양수</regType>
<regYmd>20140521</regYmd>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
<farmNo>405972</farmNo>
</item>
- <item>
<farmAddr>경상북도 김천시 대광동</farmAddr>
<farmerNm>금오산낙협</farmerNm>
<infoType>2</infoType>
<regType>도축출하</regType>
<regYmd>20150313</regYmd>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
<farmNo>342267</farmNo>
</item>
- <item>
<butcheryPlaceAddr>경상북도 고령군 다산면 송곡리</butcheryPlaceAddr>
<butcheryPlaceNm>농협고령축산물공판장경제지주주식회사</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<gradeNm>2</gradeNm>
<infoType>3</infoType>
<inspectPassYn>합격</inspectPassYn>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
</item>
- <item>
<infoType>4</infoType>
<processPlaceAddr>서울특별시 송파구 가락1동</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
</item>
- <item>
<infoType>5</infoType>
<injectionYmd>20140924</injectionYmd>
<injectiondayCnt>170일전</injectiondayCnt>
<traceNoType>CATTLE|CATTLE_NO</traceNoType>
<vaccineorder>3차</vaccineorder>
</item>
</items>
</body>
</response>
```

###### traceNoType : CATTLE/LOT_NO

**REST(URI)**

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch?ServiceKey=인증키 &traceNo=L01709271277007&optionNo=9&corpNo=1178522046
```

**응답 메시지**

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>
- <response>
- <header>
<resultCode>00</resultCode>
<resultMsg>NORMAL SERVICE.</resultMsg>
</header>
<notice />
- <body>
- <items>
- <item>
<corpNo>1348542668</corpNo>
<infoType>8</infoType>
<lotNo>LOT150317238</lotNo>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>경기도 부천시 오정구 삼정동 12-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>농협경제지주주식회사 부천축산물공판장</butcheryPlaceNm>
<butcheryYmd>20150311</butcheryYmd>
<cattleNo>410002087626476</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>충청남도 서산시 고북면 신상리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>인천광역시 서구 가좌동 477-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>삼성식품</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<cattleNo>410002084140158</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>인천광역시 강화군 불은면 신현리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>인천광역시 서구 가좌동 477-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>삼성식품</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<cattleNo>410002077720791</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>인천광역시 강화군 불은면 신현리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>인천광역시 서구 가좌동 477-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>삼성식품</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<cattleNo>410002302548357</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>강원도 철원군 갈말읍 군탄리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>인천광역시 서구 가좌동 477-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>삼성식품</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<cattleNo>410002302548316</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>강원도 철원군 갈말읍 군탄리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>경기도 안성시 일죽면 금산리 598번지</butcheryPlaceAddr>
<butcheryPlaceNm>도드람LPC</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<cattleNo>410002083720936</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>경기도 이천시 호법면 동산리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>인천광역시 서구 가좌동 477-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>삼성식품</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<cattleNo>410002077526022</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>인천광역시 강화군 불은면 신현리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>경기도 부천시 오정구 삼정동 12-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>농협경제지주주식회사 부천축산물공판장</butcheryPlaceNm>
<butcheryYmd>20150311</butcheryYmd>
<cattleNo>410002306309573</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>충청남도 서산시 고북면 신상리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>경기도 부천시 오정구 삼정동 12-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>농협경제지주주식회사 부천축산물공판장</butcheryPlaceNm>
<butcheryYmd>20150312</butcheryYmd>
<cattleNo>410002305882953</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>충청남도 청양군 남양면 온암리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>경기도 부천시 오정구 삼정동 12-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>농협경제지주주식회사 부천축산물공판장</butcheryPlaceNm>
<butcheryYmd>20150311</butcheryYmd>
<cattleNo>410002087626572</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>충청남도 서산시 고북면 신상리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>경기도 부천시 오정구 삼정동 12-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>농협경제지주주식회사 부천축산물공판장</butcheryPlaceNm>
<butcheryYmd>20150311</butcheryYmd>
<cattleNo>410002087631859</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>충청남도 서산시 고북면 신상리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>인천광역시 서구 가좌동 477-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>삼성식품</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<cattleNo>410002083275501</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>강원도 철원군 갈말읍 군탄리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>인천광역시 서구 가좌동 477-4번지</butcheryPlaceAddr>
<butcheryPlaceNm>삼성식품</butcheryPlaceNm>
<butcheryYmd>20150313</butcheryYmd>
<cattleNo>410002302432900</cattleNo>
<corpNo>1348542668</corpNo>
<farmAddr>강원도 철원군 갈말읍 군탄리</farmAddr>
<gradeNm>3</gradeNm>
<infoType>9</infoType>
<lotNo>LOT150317238</lotNo>
<lsTypeNm>육우</lsTypeNm>
<processPlaceAddr>서울특별시 송파구 가락1동 가락농수산물시장 내 대체매장 지하1층</processPlaceAddr>
<processPlaceNm>㈜농협목우촌한육우사업소</processPlaceNm>
<traceNoType>CATTLE|LOT_NO</traceNoType>
</item>
</items>
</body>
</response>
```

###### traceNoType : PIG/PIG_NO

**REST(URI)**

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch?traceNo=170003000058&optionNo=9&corpNo=6218176868&ServiceKey=인증키
```

**응답 메시지**

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>
- <response>
- <header>
<resultCode>00</resultCode>
<resultMsg>NORMAL SERVICE.</resultMsg>
</header>
<notice />
- <body>
- <items>
- <item>
<farmAddr>전라남도 화순군 능주면 남정리</farmAddr>
<farmUniqueNo>601099</farmUniqueNo>
<farmerNm>김재용</farmerNm>
<infoType>1</infoType>
<pigNo>160109900054</pigNo>
<traceNoType>PIG|PIG_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>전라남도 화순군 화순읍 서태리 6번지</butcheryPlaceAddr>
<butcheryPlaceNm>만나</butcheryPlaceNm>
<butcheryYmd>20150731</butcheryYmd>
<infoType>3</infoType>
<inspectPassYn>Y</inspectPassYn>
<pigNo>160109900054</pigNo>
<traceNoType>PIG|PIG_NO</traceNoType>
</item>
- <item>
<infoType>4</infoType>
<pigNo>160109900054</pigNo>
<processPlaceAddr>전라남도 화순군 화순읍 서태리 6</processPlaceAddr>
<processPlaceNm>㈜만나</processPlaceNm>
<processYmd>20150801</processYmd>
<traceNoType>PIG|PIG_NO</traceNoType>
</item>
</items>
</body>
</response>
```

###### traceNoType : PIG/LOT_NO

**REST(URI)**

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch?traceNo=L11507319120001&optionNo=9&corpNo=6218176868&ServiceKey=인증키
```

**응답 메시지**

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>
- <response>
- <header>
<resultCode>00</resultCode>
<resultMsg>NORMAL SERVICE.</resultMsg>
</header>
<notice />
- <body>
- <items>
- <item>
<corpNo>6218176868</corpNo>
<infoType>8</infoType>
<lotNo>L11507319120001</lotNo>
<processPlaceAddr>경상북도 영천시 도남동 695</processPlaceAddr>
<processPlaceNm>㈜산청식품</processPlaceNm>
<processYmd>20150731</processYmd>
<traceNoType>PIG|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>경상북도 영천시 도남동 695번지</butcheryPlaceAddr>
<butcheryPlaceNm>(주)삼세</butcheryPlaceNm>
<butcheryYmd>20150730</butcheryYmd>
<corpNo>6218176868</corpNo>
<farmAddr>경상북도 포항시 북구 청하면 명안리</farmAddr>
<farmerNm>강충열</farmerNm>
<infoType>9</infoType>
<pigNo>170003000058</pigNo>
<traceNoType>PIG|LOT_NO</traceNoType>
</item>
- <item>
<butcheryPlaceAddr>경상북도 영천시 도남동 695번지</butcheryPlaceAddr>
<butcheryPlaceNm>(주)삼세</butcheryPlaceNm>
<butcheryYmd>20150730</butcheryYmd>
<corpNo>6218176868</corpNo>
<farmAddr>경상북도 영천시 대창면 대창리</farmAddr>
<farmerNm>한치호</farmerNm>
<infoType>9</infoType>
<pigNo>170132400080</pigNo>
<traceNoType>PIG|LOT_NO</traceNoType>
</item>
</items>
</body>
</response>
```

###### traceNoType : FOWL/HIST_NO & DUCK/HIST_NO

**REST(URI)**

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch?traceNo=5200416040 01&optionNo=&corpNo=&ServiceKey=인증키
```

**응답 메시지**

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>
- <response>
- <header>
<resultCode>00</resultCode>
<resultMsg>NORMAL SERVICE.</resultMsg>
</header>
<notice />
- <body>
- <items>
- <item>
<brdngEnvrnNm>2(축사내 평사)</brdngEnvrnNm>
<farmAddr>전라북도 고창군 부안면</farmAddr>
<farmIdno>503317</farmIdno>
<histNo>5 200416 040 01</histNo>
<infoType>1</infoType>
<lvindRgrfc>GGJJR</lvindRgrfc>
<mngrNm>정균석</mngrNm>
<traceNoType>DUCK|HIST_NO</traceNoType>
</item>
- <item>
<abattNm>(주)삼호유황오리</abattNm>
<add0R>전라북도 정읍시 고부면</add0R>
<histNo>5 200416 040 01</histNo>
<infoType>2</infoType>
<psexmYn>합격</psexmYn>
<rceptDt>2020-04-16</rceptDt>
<traceNoType>DUCK|HIST_NO</traceNoType>
</item>
- <item>
<add0R>전라북도 정읍시 고부면</add0R>
<histNo>5 200416 040 01</histNo>
<infoType>3</infoType>
<entrpNm>(주)삼호유황오리</entrpNm>
<traceNoType>DUCK|HIST_NO</traceNoType>
</item>
</items>
</body>
</response>
```

###### traceNoType : EGG/HIST_NO

**REST(URI)**

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch?traceNo=304160070002&optionNo=&corpNo=&ServiceKey=인증키
```

**응답 메시지**

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>
- <response>
- <header>
<resultCode>00</resultCode>
<resultMsg>NORMAL SERVICE.</resultMsg>
</header>
<notice />
- <body>
- <items>
- <item>
<brdngEnvrnNm>3(개선된 케이지,0.075㎡/마리)</brdngEnvrnNm>
<edygOvpstDt>2020-04-15</edygOvpstDt>
<farmIdno>900610</farmIdno>
<frmrNm>제주초록영농조합법인</frmrNm>
<histNo>3 0416 007 0002</histNo>
<infoType>4</infoType>
<issueDt>2020-04-16</issueDt>
<lvindRgrfc>WSZQ1</lvindRgrfc>
<reqerEntrpAddr>제주특별자치도 제주시 애월읍</reqerEntrpAddr>
<reqerEntrpNm>제주초록영농조합법인</reqerEntrpNm>
<successYn>합격</successYn>
<traceNoType>EGG|HIST_NO</traceNoType>
</item>
- <item>
<entrpAddr>제주특별자치도 제주시 애월읍</entrpAddr>
<entrpNm>제주웰빙영농조합법인</entrpNm>
<histNo>3 0416 007 0002</histNo>
<infoType>5</infoType>
<traceNoType>EGG|HIST_NO</traceNoType>
</item>
</items>
</body>
</response>
```

###### traceNoType : FOWL/LOT_NO & DUCK/LOT_NO

**REST(URI)**

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace/traceNoSearch?traceNo=L22004152182001&optionNo=&corpNo=&ServiceKey=인증키
```

**응답 메시지**

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>
- <response>
- <header>
<resultCode>00</resultCode>
<resultMsg>NORMAL SERVICE.</resultMsg>
</header>
<notice />
- <body>
- <items>
- <item>
<add0R>충청북도 음성군 금왕읍</add0R>
<entrpNm>농협목우촌</entrpNm>
<infoType>1</infoType>
<lotNo>L22004152182001</lotNo>
<traceNoType>FOWL|LOT_NO</traceNoType>
</item>
- <item>
<add0R>충청남도 당진시 송악읍</add0R>
<entrpNm>(주)한국육계유통(묶음)</entrpNm>
<lotNo>L22004152182001</lotNo>
<infoType>2</infoType>
<traceNoType>FOWL|LOT_NO</traceNoType>
</item>
- <item>
<farmAddr>경상북도 봉화군 법전면</farmAddr>
<histNo>220041503401</histNo>
<infoType>3</infoType>
<lotNo>L22004152182001</lotNo>
<mngrNm>이윤재</mngrNm>
<traceNoType>FOWL|LOT_NO</traceNoType>
</item>
</items>
</body>
</response>
```

#### (2) [축산물 농장식별번호 조회] API

| API  정보 | API  번호 | 2 | API 명(국문) | 축산물농장식별번호조회 |
| --- | --- | --- | --- | --- |
|  | API  유형 | 조회 | API 명(영문) | /farmUniqueNoSearch |
|  | API 상세기능 설명 | 농장식별번호, 축종구분값을 통해 농장경영자명, 농장경영자명(또는 농장명), 사육지를 조회하는 기능 제공 |  |  |
|  | API 서비스 제공자 | 백형민 / 공공데이터포털 운영 / 044)410-7146 / carakom@ekape.or.kr |  |  |
|  | API 데이터 갱신주기 | 실시간 |  |  |
|  | Call Back URL | http://data.ekape.or.kr/openapi-data/service/user/animalTrace/farmUniqueNoSearch |  |  |
|  | 최대 메시지 사이즈 | [   bytes] |  |  |
|  | 평균 응답 시간 | [   ms] | 초당 최대 트랜잭션 | [  tps] |

##### 요청 메시지 명세

| 항목명(영문) | 항목명(국문) | 항목크기 | 항목구분 | 샘플데이터 | 항목설명 |
| --- | --- | --- | --- | --- | --- |
| farmUniqueNo | 농장식별번호 | 10 | 1 | 403419 | 농장식별번호 |
| typeGbn | 축종구분 | 10 | 1 | 0022 | 0022 : 소<br>0028 : 돼지<br>0089 : 닭.오리.계란 |
| farmerNm | 농장경영자명 | 50 | 0 |  | 농장경영자명 또는 농장명 |

※ 항목구분 : 필수(1), 옵션(0), 1건 이상 복수건(1..n), 0건 또는 복수건(0..n)

##### 응답 메시지 명세

| 항목명(영문) | 항목명(국문) | 항목크기 | 항목구분 | 샘플데이터 | 항목설명 |
| --- | --- | --- | --- | --- | --- |
| typeGbn | 축종구분 | 20 | 1 | 소 | 소 / 돼지 / 닭.오리.계란 과 같은 축종구분 |
| farmUniqueNo | 농장식별번호 | 10 | 1 | 403019 | 농장식별번호 |
| farmerNm | 농장경영자명 | 50 | 1 | 이** | 농장경영자명(비식별화를 위해 앞1자리까지만 표시) |
| addr | 사육지 주소 | 100 | 1 | 충청남도 태안군 | 사육지 주소(시군구까지 표시) |

※ 항목구분 : 필수(1), 옵션(0), 1건 이상 복수건(1..n), 0건 또는 복수건(0..n)

##### 요청 / 응답 메시지 예제

**REST(URI)**

```text
http://data.ekape.or.kr/openapi-data/service/user/animalTrace/farmUniqueNoSearch?serviceKey=인증키 &farmUniqueNo=403419&typeGbn=0022
```

**응답 메시지**

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>
- <response>
- <header>
<resultCode>00</resultCode>
<resultMsg>NORMAL SERVICE.</resultMsg>
</header>
<notice />
- <body>
- <items>
- <item>
<typeGbn>소</typeGbn>
<farmUniqueNo>403019</farmUniqueNo>
<farmerNm>이**</farmerNm>
<addr>충청남도 부여군</addr>
</item>
</items>
</body>
</response>
```

## 프로젝트 인증키 설정

- 2026-09-15 사용자 요청으로 `Config/users.XML`의 `AnimalTrace/ServiceKey`에 인증키를 보관한다. 대체 XML도 같은 구조다.
- 나머지 API 설정은 소스에서 관리하며 별도 로컬 JSON은 더 이상 사용하지 않는다. XML 배포 시 키도 포함된다.
- 인코딩된 인증키는 한 번 디코딩 후 요청용으로 인코딩한다. 실제 키는 문서에 기록하지 않는다. [설정 및 검증](소유주이력_설정_검증.md)을 참조한다.
