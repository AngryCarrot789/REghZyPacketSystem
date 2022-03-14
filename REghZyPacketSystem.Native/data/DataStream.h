#ifndef __IMPL_DATASTREAM
#define __IMPL_DATASTREAM

#include "BufferedOutStream.h"
#include "DataOutputStream.h"
#include "DataInputStream.h"
class DataStream {
public:
    DataStream(OutputStream* out, InputStream* in) {
        m_out = new BufferedOutStream(out, 128);
        m_in = in;
        m_data_out = new DataOutputStream(m_out);
        m_data_in = new DataInputStream(m_in);
    }

    DataOutputStream* getOutput() {
        return m_data_out;
    }

    DataInputStream* getInput() {
        return m_data_in;
    }

    virtual uint16_t getBytesReadable() { return -1; }

    void flushWrite() {
        m_out->flush();
    }



private:
    BufferedOutStream* m_out;
    InputStream* m_in;
    DataInputStream* m_data_in;
    DataOutputStream* m_data_out;
};

#endif // !__IMPL_DATASTREAM